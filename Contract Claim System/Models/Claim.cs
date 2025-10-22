using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Contract_Claim_System.Models
{
    public class Claim
    {
        [Key]
        public string ClaimId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string LecturerName { get; set; }

        public string LecturerDisplayName { get; set; }

        [Required]
        public string Module { get; set; }


        [Required]
        public string Period { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Hours worked must be greater than zero.")]
        public double HoursWorked { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Hourly rate must be greater than zero.")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal HourlyRate { get; set; }

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount
        {
            get => (decimal)HoursWorked * HourlyRate;
            set { }
        }

        public string? Notes { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime SubmittedDate { get; set; }

        public string SubmittedBy { get; set; }

        public string SubmittedByRole { get; set; }

        public DateTime LastModified { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime? ProcessedDate { get; set; }

        public string ApprovedBy { get; set; }

        public string RejectedBy { get; set; }

        public string DocumentData { get; set; }

        public string DocumentFileName { get; set; }

        public string DocumentContentType { get; set; }
    }
}
