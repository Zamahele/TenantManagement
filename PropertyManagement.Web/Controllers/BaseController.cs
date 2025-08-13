using Microsoft.AspNetCore.Mvc;

namespace PropertyManagement.Web.Controllers
{
    public class BaseController : Controller
    {
        protected void SetSuccessMessage(string message) => TempData["Success"] = message;
        protected void SetErrorMessage(string message) => TempData["Error"] = message;
        protected void SetInfoMessage(string message) => TempData["Info"] = message;
        protected void SetWarningMessage(string message) => TempData["Warning"] = message;

        protected void SetSidebarCounts(int? tenantCount = null, int? roomCount = null, int? pendingMaintenanceCount = null, int? waitingListCount = null)
        {
            if (tenantCount.HasValue) ViewBag.TenantCount = tenantCount.Value;
            if (roomCount.HasValue) ViewBag.RoomCount = roomCount.Value;
            if (pendingMaintenanceCount.HasValue) ViewBag.PendingMaintenanceCount = pendingMaintenanceCount.Value;
            if (waitingListCount.HasValue) ViewBag.WaitingListCount = waitingListCount.Value;
        }
    }
}