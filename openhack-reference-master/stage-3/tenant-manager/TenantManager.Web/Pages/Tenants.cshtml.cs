using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TenantManager.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TenantManager.Web.Pages 
{
    public class TenantsModel : PageModel
    {
        private readonly ITenantManager _tenantManager;

        [BindProperty]
        public string Action { get; set; }

        [BindProperty]
        public string Delete { get; set; }

        public IList<Tenant> Tenants { get; set; } = new List<Tenant>();

        public TenantsModel(ITenantManager tenantManager)
        {
            _tenantManager = tenantManager;
        }

        public async Task OnGetAsync()
        {
            Tenants = await _tenantManager.ListAllAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Action == "new")
                await _tenantManager.CreateAsync();
            else if (Action.StartsWith("delete"))
            {
                var name = Action.Replace("delete:", "");
                await _tenantManager.DeleteAsync(name);
            }

            return RedirectToPage();
        }
    }
}