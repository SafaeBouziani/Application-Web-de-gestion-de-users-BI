using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementPBI.Models
{
    public class Reports
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string? title { get; set; }
        public string? commentaire { get; set; }
        public bool is_active { get; set; } = true;

        public ICollection<Reports_Reports_BI> ReportsBIs { get; set; } = new List<Reports_Reports_BI>();
        public ICollection<Users_Reports> UsersReports { get; set; } = new List<Users_Reports>();
 
    }
}
