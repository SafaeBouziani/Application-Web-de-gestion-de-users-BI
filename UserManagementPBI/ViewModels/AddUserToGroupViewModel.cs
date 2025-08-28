namespace UserManagementPBI.ViewModels
{
    public class AddUserToGroupViewModel
    {
        public List<int> SelectedExistingUserIds { get; set; } = new();
        public int Id { get; set; }
    }
}
