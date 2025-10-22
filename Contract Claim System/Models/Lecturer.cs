namespace Contract_Claim_System.Models
{
    public class Lecturer
    {
        public string Name { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalClaims { get; set; } = 0;
    }
}