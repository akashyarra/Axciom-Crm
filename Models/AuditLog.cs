using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(150)]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [Required]
        [StringLength(100)]
        public string Module { get; set; } = string.Empty; // Customer, Lead, Opportunity, FollowUp, Activity, Auth, Security

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // Login, Failed Login, Logout, Create, Update, Delete, Role Change, Convert, Lockout

        [StringLength(100)]
        public string? EntityName { get; set; }

        [StringLength(100)]
        public string? RecordId { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Result { get; set; } = "Success"; // Success, Failed

        [StringLength(2000)]
        public string? Details { get; set; }

        [StringLength(50)]
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }
    }
}
