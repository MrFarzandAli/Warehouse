using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_Core.Controllers.IceLabourReport
{
    [Authorize]
    [Route("IceLabourReport")]
    public class IceLabourReportController : Controller
    {
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            // Redirect legacy Ice report to Warehouse Lifter & Handling Report
            return RedirectToAction("Lifter", "Warehouse");
        }
    }
}
