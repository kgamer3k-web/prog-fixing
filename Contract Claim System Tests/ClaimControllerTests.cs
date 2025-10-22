using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contract_Claim_System.Controllers;
using Contract_Claim_System.Services;
using Contract_Claim_System.Models;
using Contract_Claim_System.Data;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Contract_Claim_System_Tests
{
    public class ClaimsControllerTests
    {
        public ClaimsControllerTests()
        {
            InMemoryDb.Claims.Clear();
            InMemoryDb.Lecturers.Clear();
            InMemoryDb.StatusChanges.Clear();
        }

        private ClaimsController CreateControllerWithSession(string email = null, string role = null, IFileStorageService fileService = null)
        {
            var fileSvc = fileService ?? Mock.Of<IFileStorageService>();
            var controller = new ClaimsController(fileSvc);

            var context = new DefaultHttpContext();
            var session = new FakeSession();
            if (email != null) session.SetString("UserEmail", email);
            if (role != null) session.SetString("UserRole", role);
            context.Session = session;
            controller.ControllerContext = new ControllerContext { HttpContext = context };

            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(context, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            return controller;
        }

        [Fact]
        public void Submit_Get_RedirectsToLogin_WhenNotLoggedIn()
        {
            var controller = CreateControllerWithSession();
            var result = controller.Submit();
            Assert.IsType<RedirectToActionResult>(result);
            var r = result as RedirectToActionResult;
            Assert.Equal("Login", r.ActionName);
            Assert.Equal("Account", r.ControllerName);
        }

        [Fact]
        public void Submit_Get_ReturnsView_WithLecturerInfo_WhenLoggedIn()
        {
            InMemoryDb.Lecturers.Add(new Lecturer { Email = "lect@example.com", Name = "Lect One" });

            var controller = CreateControllerWithSession(email: "lect@example.com", role: "Lecturer");
            var result = controller.Submit();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Claim>(view.Model);
            Assert.Equal("lect@example.com", model.LecturerName);
            Assert.Equal("Lect One", model.LecturerDisplayName);
        }

        [Fact]
        public async Task Submit_Post_ReturnsView_WhenInvalidModel()
        {
            var controller = CreateControllerWithSession(email: "lect@example.com", role: "Lecturer");
            var invalidModel = new Claim { Module = "", Period = "", HoursWorked = 0, HourlyRate = 0 };
            var result = await controller.Submit(invalidModel, null);
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(invalidModel, view.Model);
            Assert.NotNull(controller.ViewBag.Error);
        }

        [Fact]
        public async Task Submit_Post_SavesClaim_WhenValid_NoFile()
        {
            var controller = CreateControllerWithSession(email: "lect@example.com", role: "Lecturer", fileService: Mock.Of<IFileStorageService>());
            var model = new Claim { Module = "M1", Period = "P1", HoursWorked = 2, HourlyRate = 10 };
            var result = await controller.Submit(model, null);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.True(InMemoryDb.Claims.Count == 1);
            var claim = InMemoryDb.Claims.First();
            Assert.Equal("Pending", claim.Status);
            Assert.Equal("lect@example.com", claim.LecturerName);
        }

        [Fact]
        public async Task Approve_Reject_AreRoleRestricted()
        {
            var created = new Claim { ClaimId = Guid.NewGuid().ToString(), LecturerName = "lect@example.com", Status = "Pending", SubmittedDate = DateTime.Now };
            InMemoryDb.Claims.Add(created);

            var controllerNoRole = CreateControllerWithSession(email: "someone@example.com", role: "Lecturer");
            var redirect = controllerNoRole.Approve(created.ClaimId) as RedirectToActionResult;
            Assert.Equal("Login", redirect.ActionName);

            var controllerAdmin = CreateControllerWithSession(email: "admin@test.com", role: "Academic Manager");
            var result = controllerAdmin.Approve(created.ClaimId);
            var updated = InMemoryDb.Claims.First(c => c.ClaimId == created.ClaimId);
            Assert.Equal("Approved", updated.Status);
            Assert.Equal("admin@test.com", updated.ApprovedBy);
        }
    }
}