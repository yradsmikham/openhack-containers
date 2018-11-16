﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TenantManager.Core;

namespace TenantManager.Web.Controllers
{
    [Route("api/[controller]")]
    public class TenantsController : Controller
    {
        private readonly ITenantManager _tenantManager;

        public TenantsController(ITenantManager tenantManager)
        {
            _tenantManager = tenantManager;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var tenants = await _tenantManager.ListAllAsync();
            return Ok(tenants);
        }
        
        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            await _tenantManager.CreateAsync();
            return Ok();
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteAsync(string name)
        {
            await _tenantManager.DeleteAsync(name);
            return Ok();
        }
    }
}