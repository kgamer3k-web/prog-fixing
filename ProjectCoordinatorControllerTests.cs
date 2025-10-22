using Xunit;
using Microsoft.AspNetCore.Mvc;
using Contract_Claim_System.Controllers;
using Contract_Claim_System.Models;
using Contract_Claim_System.Data;
using System;
using System.Linq;

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
    public void Index_FiltersByStatus()
    {
        InMemoryDb.Claims.Add(new Claim { ClaimId = "1", Status = "Pending", SubmittedDate = DateTime.Now.AddMinutes(-1) });
        InMemoryDb.Claims.Add(new Claim { ClaimId = "2", Status = "Approved", SubmittedDate = DateTime.Now });

        var controller = CreateControllerWithSession();
        var resultAll = controller.Index(null) as ViewResult;
        var listAll = Assert.IsType<System.Collections.Generic.List<Claim>>(resultAll.Model);
        Assert.Equal(2, listAll.Count);

        var resultApproved = controller.Index("Approved") as ViewResult;
        var listApproved = Assert.IsType<System.Collections.Generic.List<Claim>>(resultApproved.Model);
        Assert.Single(listApproved);
        Assert.Equal("Approved", listApproved.First().Status);
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