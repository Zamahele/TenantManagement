using Microsoft.AspNetCore.Mvc;

namespace PropertyManagement.Web.Controllers
{
    public class BaseController : Controller
    {
        protected void SetSuccessMessage(string message) => TempData["Success"] = message;
        protected void SetErrorMessage(string message) => TempData["Error"] = message;
        protected void SetInfoMessage(string message) => TempData["Info"] = message;
        protected void SetWarningMessage(string message) => TempData["Warning"] = message;
    }
}