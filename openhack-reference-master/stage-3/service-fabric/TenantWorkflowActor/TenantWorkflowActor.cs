using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Refit;
using Stateless;
using TenantManager.Common.Actors;
using TenantManager.Common.Services;
using TenantManager.Common.Utilities;

namespace TenantWorkflowActor
{

    [StatePersistence(StatePersistence.Persisted)]
    internal class TenantWorkflowActor : Actor, ITenantWorkflowActor
    {
        private StateMachine<State, Trigger> machine;
        private StateMachine<State, Trigger>.TriggerWithParameters<string, string> createExternalIpTrigger;
        private StateMachine<State, Trigger>.TriggerWithParameters<string> createTrigger;

        private State state;

        
        enum Trigger { CreateApplication, InternalEndpointCreated , CreateExternalIp, ExternalEndpointCreated, Delete,
            Deleted, Error  
        }

        public TenantWorkflowActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            ActorEventSource.Current.ActorMessage(this, $"Contructored called: {this.GetActorId()}");
        }

        protected override Task OnDeactivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, $"Actor deactivated: {this.GetActorId()}");
            ActorEventSource.Current.ActorMessage(this, $"Actor State deactivated: {this.GetActorId()}, {this.state}");

            return base.OnDeactivateAsync();
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>t
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, $"Actor activated: {this.GetActorId()}");

            var savedState = await this.StateManager.TryGetStateAsync<State>("state");
            if (savedState.HasValue)
            {
                // has started processing
                this.state = savedState.Value;
            }
            else
            {
                // first load ever initalize
                this.state = State.New;
                await this.StateManager.SetStateAsync<State>("state", this.state);
                await this.StateManager.SetStateAsync<StatusHistory>("statusHistory", new StatusHistory(this.state));
            }

            ActorEventSource.Current.ActorMessage(this, $"Actor state at activation: {this.GetActorId()}, {this.state}");

            machine = new StateMachine<State, Trigger>(() => this.state, s => this.state = s);
      
            machine.OnTransitionedAsync(LogTransitionAsync);
            // dont throw if unhandled trigger
            machine.OnUnhandledTrigger((s, trigger) => { ActorEventSource.Current.ActorMessage(this, $"TenantWorkflowActor Trigger was called but not handled. Actor: {this.GetActorId()} State: {s} Trigger: {trigger}"); });

            createTrigger = machine.SetTriggerParameters<string>(Trigger.CreateApplication);
            createExternalIpTrigger = machine.SetTriggerParameters<string, string>(Trigger.CreateExternalIp);

            machine.Configure(State.New)
                .Permit(Trigger.CreateApplication, State.Creating)
                .Permit(Trigger.Delete, State.Deleted);


            machine.Configure(State.Creating)
                .OnEntryFromAsync(createTrigger, tenantName => OnCreate(tenantName))
                .Permit(Trigger.InternalEndpointCreated, State.InternalReady)
                .Permit(Trigger.Delete, State.Deleted)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.InternalReady)
                .Permit(Trigger.CreateExternalIp, State.ExternalIpCreating)
                .Permit(Trigger.Delete, State.Deleted)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.ExternalIpCreating)
                //.OnEntryAsync() todo wire up time out notifcation
                .OnEntryFromAsync(createExternalIpTrigger, (applicationName, rconEndpoint) => OnCreateExternalIp(applicationName, rconEndpoint))
                .Permit(Trigger.ExternalEndpointCreated, State.CustomerReady)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.CustomerReady)
                .Permit(Trigger.Delete, State.Deleting)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.Deleting)
                .OnEntryAsync(() => OnDelete())
                .Permit(Trigger.Deleted, State.Deleted)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.Deleted)
                .OnEntryAsync(() => FinishDeleting());

            machine.Configure(State.Error)
                .OnEntryAsync(() => OnError());
        }

        private async Task LogTransitionAsync(StateMachine<State, Trigger>.Transition arg)
        {
            var conditionalValue = await this.StateManager.TryGetStateAsync<StatusHistory>("statusHistory");

            StatusHistory history;
            if (conditionalValue.HasValue)
            {
                history = StatusHistory.AddNewStatus(arg.Destination, conditionalValue.Value);
            }
            else
            {
                history = new StatusHistory(arg.Destination);
            }

            await this.StateManager.SetStateAsync<StatusHistory>("statusHistory", history);
        }

        private Task OnError()
        {
            ActorEventSource.Current.ActorMessage(this, $"Actor entered error state: {this.GetActorId()}, {this.state}");
            return Task.FromResult(true);
        }


        private Task FinishDeleting()
        {
            throw new NotImplementedException();
        }

        private Task OnDelete()
        {
            ActorEventSource.Current.ActorMessage(this, $"Actor deletion called: {this.GetActorId()}, {this.state}");
            return Task.FromResult(true);
        }

        private async Task OnCreateExternalIp(string internalIpAddress, string rconEndpoint)
        {
            var servicePartitionKey = new ServicePartitionKey(internalIpAddress.GetPartionKey());
            var tenantName = await this.StateManager.GetStateAsync<string>("tenantName");

            IIpManager ipManager = ServiceProxy.Create<IIpManager>(new Uri("fabric:/MultiTenant/IpManager"), servicePartitionKey);

            try
            {
                await ipManager.QueueIpCreation(tenantName, internalIpAddress, rconEndpoint);
            }
            catch (Exception e)
            {
                await machine.FireAsync(Trigger.Error);
            }
        }


        async Task OnCreate(string tenantName)
        {
            await this.StateManager.SetStateAsync<string>("tenantName", tenantName);
            ITenantManager tenantMangerApi = ServiceProxy.Create<ITenantManager>(new Uri("fabric:/MultiTenant/TenantManager"));

            try
            {
                await tenantMangerApi.QueueTenantCreation(tenantName);
            }
            catch (Exception e)
            {
              await machine.FireAsync(Trigger.Error);
            }
        }

        public async Task<TenantStatus> Create(string tenantName)
        {
            await machine.FireAsync(createTrigger, tenantName);
            return await GetStatus();
        }

        public async Task<TenantStatus> InternalCreated(string internalIp, string rconEndpoint)
        {
            await this.StateManager.SetStateAsync<string>("internalIp", internalIp);
            await this.StateManager.SetStateAsync<string>("rconEndpoint", rconEndpoint);
            await machine.FireAsync(Trigger.InternalEndpointCreated);
            await machine.FireAsync(createExternalIpTrigger, internalIp, rconEndpoint);

            return await GetStatus();
        }

        public async Task<TenantStatus> ExternalCreated(string externalEndpoint)
        {
            await this.StateManager.SetStateAsync<string>("externalEndpoint", externalEndpoint);
            await machine.FireAsync(Trigger.ExternalEndpointCreated);
            return await GetStatus();
        }

        public async Task<TenantStatus> Delete()
        {
            await machine.FireAsync(Trigger.Delete);
            return await GetStatus();
        }

        public async Task<TenantStatus> GetStatus()
        {
            var status = await this.StateManager.TryGetStateAsync<StatusHistory>("statusHistory");
            var internalIp = await this.StateManager.TryGetStateAsync<string>("internalIp");
            var tenantName = await this.StateManager.TryGetStateAsync<string>("tenantName");
            var externalEndpoint = await this.StateManager.TryGetStateAsync<string>("externalEndpoint");
            var rconEndpoint = await this.StateManager.TryGetStateAsync<string>("rconEndpoint");


            var tenantStatus = new TenantStatus(machine.State);
            tenantStatus.History = status.HasValue ? status.Value : new StatusHistory(machine.State);
            tenantStatus.TenantName = tenantName.HasValue ? tenantName.Value : string.Empty;
            tenantStatus.InternalIp = internalIp.HasValue ? internalIp.Value : string.Empty;
            tenantStatus.ExternalEndPoint = externalEndpoint.HasValue ? externalEndpoint.Value : string.Empty;
            tenantStatus.RconEndpoint = rconEndpoint.HasValue ? rconEndpoint.Value : string.Empty;


            return tenantStatus;
        }
    }
}
