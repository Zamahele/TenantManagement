using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Test.Infrastructure;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace PropertyManagement.Test.Infrastructure
{
    /// <summary>
    /// Base class for all controller tests providing common setup and utility methods
    /// </summary>
    public abstract class BaseControllerTest
    {
        protected IMapper Mapper { get; }

        protected BaseControllerTest()
        {
            Mapper = TestMappingProfiles.GetTestMapper();
        }

        /// <summary>
        /// Creates an in-memory database context for testing
        /// </summary>
        protected ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        /// <summary>
        /// Creates a ClaimsPrincipal for testing with specified role and user info
        /// </summary>
        protected ClaimsPrincipal GetTestUser(string role = "Tenant", int userId = 1, string username = "testuser")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Sets up controller context with user authentication and TempData
        /// </summary>
        protected void SetupControllerContext(Controller controller, ClaimsPrincipal user = null)
        {
            user ??= GetTestUser();
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            controller.TempData = new TempDataDictionary(
                controller.ControllerContext.HttpContext, 
                Mock.Of<ITempDataProvider>());
        }

        /// <summary>
        /// Creates a manager user for testing manager-specific functionality
        /// </summary>
        protected ClaimsPrincipal GetManagerUser(int userId = 1, string username = "manager")
        {
            return GetTestUser("Manager", userId, username);
        }

        /// <summary>
        /// Creates a tenant user for testing tenant-specific functionality
        /// </summary>
        protected ClaimsPrincipal GetTenantUser(int userId = 2, string username = "tenant")
        {
            return GetTestUser("Tenant", userId, username);
        }
    }
}