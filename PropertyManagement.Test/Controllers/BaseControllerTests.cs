using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using PropertyManagement.Web.Controllers;
using Moq;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers;

public class BaseControllerTests
{
    // Expose protected methods for testing
    private class TestController : BaseController
    {
        public void CallSetSuccessMessage(string message) => SetSuccessMessage(message);
        public void CallSetErrorMessage(string message) => SetErrorMessage(message);
        public void CallSetInfoMessage(string message) => SetInfoMessage(message);
        public void CallSetWarningMessage(string message) => SetWarningMessage(message);
    }

    private TestController GetController()
    {
        var controller = new TestController();
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        return controller;
    }

    [Fact]
    public void SetSuccessMessage_SetsTempData()
    {
        var controller = GetController();
        controller.CallSetSuccessMessage("ok");
        Assert.Equal("ok", controller.TempData["Success"]);
    }

    [Fact]
    public void SetErrorMessage_SetsTempData()
    {
        var controller = GetController();
        controller.CallSetErrorMessage("fail");
        Assert.Equal("fail", controller.TempData["Error"]);
    }

    [Fact]
    public void SetInfoMessage_SetsTempData()
    {
        var controller = GetController();
        controller.CallSetInfoMessage("info");
        Assert.Equal("info", controller.TempData["Info"]);
    }

    [Fact]
    public void SetWarningMessage_SetsTempData()
    {
        var controller = GetController();
        controller.CallSetWarningMessage("warn");
        Assert.Equal("warn", controller.TempData["Warning"]);
    }
}