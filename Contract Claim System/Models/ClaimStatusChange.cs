namespace Contract_Claim_System.Models
{
    public class ClaimStatusChange
    {
        public string ClaimId { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public string LecturerDisplayName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PreviousStatus { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedDate { get; set; }
    }
}