using Xunit;
using Microsoft.AspNetCore.Mvc;
using Contract_Claim_System.Controllers;
using Contract_Claim_System.Models;
using System.IO;

namespace Contract_Claim_System_Tests
{
    public class AccountControllerTests
    {
        [Fact]
        public void Login_Fails_WithInvalidCredentials()
        {
            var controller = new AccountController();
            var result = controller.Login("noone@example.com", "badpass");
            var view = Assert.IsType<ViewResult>(result);
            Assert.NotNull(controller.ViewBag.Error);
        }

        [Fact]
        public void Register_PreventsDuplicateEmail()
        {
            Directory.CreateDirectory("Data");
            var lecturerPath = "Data/lecturers.txt";
            File.WriteAllText(lecturerPath, "existing@test.com,pass\n");

            var controller = new AccountController();
            var newUser = new User { Email = "existing@test.com", Password = "pass", Role = "Lecturer" };
            var result = controller.Register(newUser);
            var view = Assert.IsType<ViewResult>(result);
            Assert.NotNull(controller.ViewBag.Error);
        }
    }
}