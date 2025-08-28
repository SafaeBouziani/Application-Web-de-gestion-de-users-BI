namespace UserManagementPBI.ViewModels
{
    public class AssignGroupsToUserViewModel
    {
        public int Id { get; set; }
        public List<int> SelectedExistingGroupIds { get; set; } = new();
        public List<GroupCreateViewModel> NewGroups { get; set; } = new();
        public List<GroupCreateViewModel> Groups { get; set; } = new();
    }
}
