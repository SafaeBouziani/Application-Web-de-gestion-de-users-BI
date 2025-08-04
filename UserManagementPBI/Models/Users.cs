using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementPBI.Models
{
    public class Users
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string? userName { get; set; }
        public string? pwd { get; set; }
        public string? role { get; set; }
        public string? client { get; set; }
        public string? mail { get; set; }
        public string? view_user { get; set; }
        public DateTime? last_failed_utc_datetime { get; set; }
        public int failed_times { get; set; } = 0;
        public DateTime DateCreation { get; set; }  
        public DateTime? DateModification { get; set; }
        public string? CreatedByAdminId { get; set; }
        public Admins? CreatedByAdmin { get; set; }
        // Additional properties can be added as needed
    }
  

}
