using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Contract_Claim_System.Controllers;
using Contract_Claim_System.Models;
using Contract_Claim_System.Data;
using System;
using System.Linq;

namespace Contract_Claim_System_Tests
{
    public class ProjectCoordinatorControllerTests
    {
        public ProjectCoordinatorControllerTests()
        {
            InMemoryDb.Claims.Clear();
            InMemoryDb.StatusChanges.Clear();
        }

        private ProjectCoordinatorController CreateControllerWithSession(string email = "coord@test.com", string role = "Project Coordinator")
        {
            var controller = new ProjectCoordinatorController();
            var context = new DefaultHttpContext();
            var session = new FakeSession();
            session.SetString("UserEmail", email);
            session.SetString("UserRole", role);
            context.Session = session;
            controller.ControllerContext = new ControllerContext { HttpContext = context };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(context, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            return controller;
        }


        [Fact]
        public void Edit_Post_UpdatesClaim()
        {
            var claim = new Claim { ClaimId = "C1", Module = "Old", Period = "P", HoursWorked = 1, HourlyRate = 1m };
            InMemoryDb.Claims.Add(claim);

            var controller = CreateControllerWithSession();
            claim.Module = "New";
            var result = controller.Edit(claim) as RedirectToActionResult;
            Assert.Equal("Details", result.ActionName);
            var updated = InMemoryDb.Claims.First(c => c.ClaimId == "C1");
            Assert.Equal("New", updated.Module);
        }
    }
}