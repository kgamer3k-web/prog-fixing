using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Contract_Claim_System.Models;

namespace Contract_Claim_System.Data
{
    public static partial class InMemoryDb
    {
        public static List<User> Users { get; set; } = new();
        public static List<Claim> Claims { get; set; } = new();
        public static List<Lecturer> Lecturers { get; set; } = new();
        public static List<ClaimStatusChange> StatusChanges { get; set; } = new();

        public static List<Claim> GetClaimsByLecturer(string lecturerEmail)
        {
            return Claims.Where(c => c.LecturerName.Equals(lecturerEmail, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public static List<ClaimStatusChange> GetStatusChangesByClaimId(string claimId)
        {
            return StatusChanges
                .Where(sc => sc.ClaimId == claimId)
                .OrderBy(sc => sc.ChangedDate)
                .ToList();
        }
    }
}