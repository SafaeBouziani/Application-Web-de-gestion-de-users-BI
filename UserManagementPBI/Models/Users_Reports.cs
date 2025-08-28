namespace UserManagementPBI.Models
{
    public class Users_Reports
    {
        public int id_users { get; set; }
        public Users User { get; set; }

        public int id_reports { get; set; }
        public Reports Report { get; set; }

        public int? order_report { get; set; }
    }
}
