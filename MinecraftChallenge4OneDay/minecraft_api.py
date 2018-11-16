#!flask/bin/python
from flask import Flask, request, jsonify
from kubernetes import client, config
import pandas as pd
import json
import time
from pprint import pprint
import kubernetes

app = Flask(__name__)
app.config["DEBUG"] = True

@app.route('/', methods=['GET'])
def headline():
    return'''<h1> Kubernetes Cluster </h1>
<p>A prototype API for deploying, scaling, and upgrading your cluster.</p>'''

@app.route('/api/v1/pods', methods=['GET'])
def list_pods():
    df = pd.DataFrame(columns=['pod_ip','namespace','name'])
    # Configs can be set in Configuration class directly or using helper utility
    config.load_kube_config()
    v1 = client.CoreV1Api()
    print("Listing pods with their IPs:")
    api_response = v1.list_pod_for_all_namespaces(watch=False)
    for i in api_response.items:
        df = df.append({'pod_ip': i.status.pod_ip, 'namespace': i.metadata.namespace, 'name': i.metadata.name }, ignore_index=True)
    dfstr = df.to_json()
    return(dfstr)

@app.route('/api/v1/nodes', methods=['GET'])
def list_nodes():
    df = pd.DataFrame(columns=['node_name','namespace'])
    config.load_kube_config()
    v1 = client.CoreV1Api()

    #configuration = kubernetes.client.Configuration()
    #configuration.api_key_prefix['authorization'] = 'Bearer'
    #configuration.api_key['authorization'] = 'eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJrdWJlcm5ldGVzL3NlcnZpY2VhY2NvdW50Iiwia3ViZXJuZXRlcy5pby9zZXJ2aWNlYWNjb3VudC9uYW1lc3BhY2UiOiJkZWZhdWx0Iiwia3ViZXJuZXRlcy5pby9zZXJ2aWNlYWNjb3VudC9zZWNyZXQubmFtZSI6ImRlZmF1bHQtdG9rZW4tZnpsNmsiLCJrdWJlcm5ldGVzLmlvL3NlcnZpY2VhY2NvdW50L3NlcnZpY2UtYWNjb3VudC5uYW1lIjoiZGVmYXVsdCIsImt1YmVybmV0ZXMuaW8vc2VydmljZWFjY291bnQvc2VydmljZS1hY2NvdW50LnVpZCI6ImJlZjMyZjc4LWU0NzktMTFlOC04N2QzLTYyYmE1YWY1OWE5NyIsInN1YiI6InN5c3RlbTpzZXJ2aWNlYWNjb3VudDpkZWZhdWx0OmRlZmF1bHQifQ.Tg3_FHy_EpokHhI1Bv2uXe7iFcAqywD4NO8eo0r9xksLKRleXloqfDbT6J-bMcxmKi-KKtZcxDxJdN2I7j0D8z0POJGXq-w1osdFJHcE2nH7ZmWSNmrvDp2jNQJP-d_zAXvqvBV_QHte-h6ecc_2C2ioX5_eSVirliyaKh8bIKLLCxxQkOt2xYmKU5M-_Z07umEEogYP_fbh7aWeaud3Rih5VIowCtw60mVC4FG3Az0tEWvpS4ZrcR8HFhEdSCDIHPN9kErk7d6LDHSVBQJB51VRX6xkhKCn2uJtBridJut3HdMK7yc6DWaGKWgZbZ6QT_j-I0BqScoj_ct1qSi0zuw3Nga34yig1AQ4q7jCgL7RAC7cPS_7uKfCLV5p7fbqx5r26M_H_mWi9OPoK_kXsg3YSpMwTeDJmYO1C34z7kjUZTXNcYPhyz7RrZhVxYgrQ_OLHLfb_jeLEXL9ulU03S7RCM0LMzqqYSuXjZbv55LcXsy9JyrMHchVJ_fucidwPSP0qfoN0ZR-ruFTJh3C0ygo6mdfO2kARiLGlFr-t6WhZgIg3PYZgECKbhMPAIiQ9uwVv69NF_GQM2SSDvdft2L9NJ5_F_o6ihqZCXVn-J9_0dhFkpdApr_aP95jpN2ZHfERJ9KW8cabvFR66i21-dG2yuX7fMRzuroRr9P-i_s'
    #configuration.api_key['authorization'] = '4608cc3712d26e573725d6c3d4d1c185'
    #api_instance = kubernetes.client.CoreV1Api(kubernetes.client.ApiClient(configuration))

    print("Listing nodes in Kubernetes cluster")
    api_response = v1.list_node(watch=False)
    #api_response = api_instance.list_node()
    #api_response_json = json.dumps(api_response, sort_keys=True, indent=4)

    for i in api_response.items:
        df = df.append({'node_name': i.metadata.name, 'namespace': i.metadata.namespace}, ignore_index=True)
    dfstr = df.to_json()
    return(dfstr)

@app.route('/api/v1/delete', methods=['DELETE'])
def delete_instance():
    config.load_kube_config()
    v1 = client.CoreV1Api()

    # create an instance of the API class
    name = 'test-pod'
    namespace = 'default' # str | object name and auth scope, such as for teams and projects
    body = client.V1Pod() # V1Pod | 
    pretty = 'pretty_example' # str | If 'true', then the output is pretty printed. (optional)
    dry_run = 'dry_run_example' # str | When present, indicates that modifications should not be persisted. An invalid or unrecognized dryRun directive will result in an error response and no further processing of the request. Valid values are: - All: all dry run stages will be processed (optional)
    grace_period_seconds = 56 # int | The duration in seconds before the object should be deleted. Value must be non-negative integer. The value zero indicates delete immediately. If this value is nil, the default grace period for the specified type will be used. Defaults to a per object value if not specified. zero means delete immediately. (optional)
    orphan_dependents = True # bool | Deprecated: please use the PropagationPolicy, this field will be deprecated in 1.7. Should the dependent objects be orphaned. If true/false, the \"orphan\" finalizer will be added to/removed from the object's finalizers list. Either this field or PropagationPolicy may be set, but not both. (optional)
    
    print("Deleting a Pod in Kubernetes Cluster")
    api_response = v1.delete_namespaced_pod(name, namespace, body, pretty=pretty, dry_run=dry_run, grace_period_seconds=grace_period_seconds, orphan_dependents=orphan_dependents)
    #json_ret = json.dumps(ret,sort_keys=True, indent=4)
    return(api_response)

@app.route('/api/v1/add', methods=['GET'])
def add_instance():
    config.load_kube_config()
    v1 = client.CoreV1Api()

    pod=client.V1Pod()
    pod.metadata=client.V1ObjectMeta(name="minecraft")
    spec=client.V1PodSpec()
    container=client.V1Container()
    container.image="openhack/minecraft-server"
    container.args=["EULA", "TRUE"]
    container.name="minecraft"

    spec.containers = [container]
    pod.spec = spec

    # create an instance of the API class
    namespace = 'default' # str | object name and auth scope, such as for teams and projects
    #body = client.V1Pod(metadata.name='test', spec.containers.name='minecraft', spec.containers.image='openhack/minecraft-server:2.0' ) # V1Pod | 
    include_uninitialized = True # bool | If true, partially initialized resources are included in the response. (optional)
    pretty = 'pretty_example' # str | If 'true', then the output is pretty printed. (optional)
    dry_run = 'dry_run_example' # str | When present, indicates that modifications should not be persisted. An invalid or unrecognized dryRun directive will result in an error response and no further processing of the request. Valid values are: - All: all dry run stages will be processed (optional)

    print("Creating a Pod in Kubernetes Cluster")
    api_response = v1.create_namespaced_pod(namespace, pod, include_uninitialized=include_uninitialized, pretty=pretty, dry_run=dry_run)
    #json_ret = json.dumps(ret,sort_keys=True, indent=4)
    return(api_response)

app.run()
