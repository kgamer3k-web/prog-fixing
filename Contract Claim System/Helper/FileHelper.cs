using Contract_Claim_System.Models;
using Contract_Claim_System.Data;
using System.Text.Json;

namespace Contract_Claim_System.Helper
{
    public static class FileHelper
    {
        private static readonly string lecturerFile = "Data/lecturers.txt";
        private static readonly string adminFile = "Data/admin.txt";
        private static readonly string coordinatorFile = "Data/coordinators.txt";
        private static readonly string claimsFile = "Data/claims.json";
        private static readonly string statusChangesFile = "Data/status_changes.json";

        public static void EnsureFilesExist()
        {
            Directory.CreateDirectory("Data");

            if (!File.Exists(lecturerFile))
                File.Create(lecturerFile).Close();

            if (!File.Exists(adminFile))
                File.Create(adminFile).Close();

            if (!File.Exists(coordinatorFile))
                File.Create(coordinatorFile).Close();

            if (!File.Exists(claimsFile))
                File.WriteAllText(claimsFile, "[]");

            if (!File.Exists(statusChangesFile))
                File.WriteAllText(statusChangesFile, "[]");
        }

        public static void InitializeTestUsers()
        {
            if (!File.ReadAllText(adminFile).Contains("admin@test.com"))
            {
                File.AppendAllText(adminFile, "admin@test.com,admin123\n");
            }

            if (!File.ReadAllText(lecturerFile).Contains("lecturer@test.com"))
            {
                File.AppendAllText(lecturerFile, "lecturer@test.com,lecturer123\n");
            }

            if (!File.ReadAllText(coordinatorFile).Contains("coordinator@test.com"))
            {
                File.AppendAllText(coordinatorFile, "coordinator@test.com,coordinator123\n");
            }
        }

        public static void LoadData()
        {
            InMemoryDb.Users.Clear();
            InMemoryDb.Claims.Clear();

            // Load Lecturers
            if (File.Exists(lecturerFile))
            {
                foreach (var line in File.ReadAllLines(lecturerFile))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        InMemoryDb.Users.Add(new User
                        {
                            Email = parts[0],
                            Password = parts[1],
                            Role = "Lecturer"
                        });
                    }
                }
            }

            // Load Academic Managers
            if (File.Exists(adminFile))
            {
                foreach (var line in File.ReadAllLines(adminFile))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        InMemoryDb.Users.Add(new User
                        {
                            Email = parts[0],
                            Password = parts[1],
                            Role = "Academic Manager"
                        });
                    }
                }
            }

            // Load Project Coordinators
            if (File.Exists(coordinatorFile))
            {
                foreach (var line in File.ReadAllLines(coordinatorFile))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        InMemoryDb.Users.Add(new User
                        {
                            Email = parts[0],
                            Password = parts[1],
                            Role = "Project Coordinator"
                        });
                    }
                }
            }

            // Load Claims
            if (File.Exists(claimsFile))
            {
                var json = File.ReadAllText(claimsFile);
                var claims = JsonSerializer.Deserialize<List<Claim>>(json);
                if (claims != null)
                {
                    InMemoryDb.Claims = claims;
                }
            }
        }

        public static List<Claim> LoadClaims()
        {
            if (!File.Exists(claimsFile))
                return new List<Claim>();

            var json = File.ReadAllText(claimsFile);
            return JsonSerializer.Deserialize<List<Claim>>(json) ?? new List<Claim>();
        }

        public static void WriteToFile(string path, List<Claim> claims)
        {
            var json = JsonSerializer.Serialize(claims, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static List<ClaimStatusChange> LoadStatusChanges()
        {
            if (!File.Exists(statusChangesFile))
                return new List<ClaimStatusChange>();

            var json = File.ReadAllText(statusChangesFile);
            return JsonSerializer.Deserialize<List<ClaimStatusChange>>(json) ?? new List<ClaimStatusChange>();
        }

        public static void SaveStatusChanges(List<ClaimStatusChange> statusChanges)
        {
            Directory.CreateDirectory("Data");
            var json = JsonSerializer.Serialize(statusChanges, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(statusChangesFile, json);
        }
    }
}