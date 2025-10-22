using Contract_Claim_System.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Contract_Claim_System.Data;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;

namespace Contract_Claim_System.Controllers
{
    public class ProjectCoordinatorController : Controller
    {
        private readonly string claimsFile = "Data/claims.json";
        private readonly string statusChangesFile = "Data/status_changes.json";

        [HttpGet]
        public IActionResult Index(string status)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(role) || role != "Project Coordinator")
            {
                TempData["Error"] = "Access denied. Project Coordinator role required.";
                return RedirectToAction("Login", "Account");
            }

            // Start with all claims
            var claims = InMemoryDb.Claims.AsQueryable();

            // Apply status filter
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                claims = claims.Where(c => c.Status == status);
            }

            var filteredClaims = claims.OrderByDescending(c => c.SubmittedDate).ToList();

            // Pass the status value to the view - handle null/empty as "All"
            ViewBag.SelectedStatus = string.IsNullOrEmpty(status) ? "All" : status;

            return View(filteredClaims);
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role) || role != "Project Coordinator")
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var claim = InMemoryDb.Claims.FirstOrDefault(c => c.ClaimId == id);
            if (claim == null)
                return NotFound();

            var statusChanges = LoadStatusChanges()
                .Where(sc => sc.ClaimId == id)
                .OrderByDescending(sc => sc.ChangedDate)
                .ToList();

            ViewBag.StatusChanges = statusChanges;

            return View(claim);
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role) || role != "Project Coordinator")
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var claim = InMemoryDb.Claims.FirstOrDefault(c => c.ClaimId == id);
            if (claim == null)
                return NotFound();

            return View(claim);
        }

        [HttpPost]
        public IActionResult Edit(Claim claim)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(role) || role != "Project Coordinator")
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(claim.Module) ||
                string.IsNullOrWhiteSpace(claim.Period) ||
                claim.HoursWorked <= 0 ||
                claim.HourlyRate <= 0)
            {
                ViewBag.Error = "Please fill in all required fields with valid values.";
                return View(claim);
            }

            var existingClaim = InMemoryDb.Claims.FirstOrDefault(c => c.ClaimId == claim.ClaimId);
            if (existingClaim == null)
                return NotFound();

            // Update claim fields
            existingClaim.Module = claim.Module;
            existingClaim.Period = claim.Period;
            existingClaim.HoursWorked = claim.HoursWorked;
            existingClaim.HourlyRate = claim.HourlyRate;
            existingClaim.Notes = claim.Notes;
            existingClaim.LastModified = DateTime.Now;
            existingClaim.ModifiedBy = email ?? "Project Coordinator";

            SaveClaims(InMemoryDb.Claims);

            TempData["Message"] = "Claim updated successfully!";

            return RedirectToAction("Details", new { id = claim.ClaimId });
        }

        [HttpGet]
        public IActionResult ChangeStatus(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role) || role != "Project Coordinator")
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var claim = InMemoryDb.Claims.FirstOrDefault(c => c.ClaimId == id);
            if (claim == null)
                return NotFound();

            var model = new ClaimStatusChange
            {
                ClaimId = id,
                CurrentStatus = claim.Status,
                LecturerName = claim.LecturerName,
                LecturerDisplayName = claim.LecturerDisplayName,
                Module = claim.Module,
                TotalAmount = claim.TotalAmount
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult ChangeStatus(ClaimStatusChange statusChange)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(role) || role != "Project Coordinator")
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(statusChange.NewStatus))
            {
                ViewBag.Error = "Please select a new status.";
                return View(statusChange);
            }

            if (statusChange.NewStatus == statusChange.CurrentStatus)
            {
                ViewBag.Error = "New status cannot be the same as current status.";
                return View(statusChange);
            }

            if (string.IsNullOrWhiteSpace(statusChange.Reason))
            {
                ViewBag.Error = "Reason for status change is required.";
                return View(statusChange);
            }

            var claim = InMemoryDb.Claims.FirstOrDefault(c => c.ClaimId == statusChange.ClaimId);
            if (claim == null)
                return NotFound();

            // Record status change
            statusChange.ChangedBy = email ?? "Project Coordinator";
            statusChange.ChangedDate = DateTime.Now;
            statusChange.PreviousStatus = claim.Status;

            var statusChanges = LoadStatusChanges();
            statusChanges.Add(statusChange);
            SaveStatusChanges(statusChanges);

            // Update claim status
            claim.Status = statusChange.NewStatus;
            claim.LastModified = DateTime.Now;
            claim.ModifiedBy = email ?? "Project Coordinator";
            claim.ProcessedDate = DateTime.Now;

            if (statusChange.NewStatus == "Approved")
            {
                claim.ApprovedBy = email;
                claim.RejectedBy = null;
            }
            else if (statusChange.NewStatus == "Rejected")
            {
                claim.RejectedBy = email;
                claim.ApprovedBy = null;
            }
            else if (statusChange.NewStatus == "Pending")
            {
                claim.ApprovedBy = null;
                claim.RejectedBy = null;
            }

            SaveClaims(InMemoryDb.Claims);

            TempData["Message"] = $"Claim status changed to {statusChange.NewStatus} successfully!";
            return RedirectToAction("Details", new { id = statusChange.ClaimId });
        }

        private void SaveClaims(List<Claim> claims)
        {
            Directory.CreateDirectory("Data");
            var json = JsonSerializer.Serialize(claims, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(claimsFile, json);
        }

        private List<ClaimStatusChange> LoadStatusChanges()
        {
            if (!System.IO.File.Exists(statusChangesFile))
                return new List<ClaimStatusChange>();

            var json = System.IO.File.ReadAllText(statusChangesFile);
            return JsonSerializer.Deserialize<List<ClaimStatusChange>>(json) ?? new List<ClaimStatusChange>();
        }

        private void SaveStatusChanges(List<ClaimStatusChange> changes)
        {
            Directory.CreateDirectory("Data");
            var json = JsonSerializer.Serialize(changes, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(statusChangesFile, json);
        }
    }
}