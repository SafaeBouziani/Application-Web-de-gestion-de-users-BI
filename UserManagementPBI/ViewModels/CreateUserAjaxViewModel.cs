using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UserManagementPBI.ViewModels
{
    public class CreateUserAjaxViewModel
    {
        public int Id { get; set; } // User ID, used for editing existing users
        // Step 1
        [Required]
        public string UserName { get; set; }
        public string BIUserRole { get; set; }
        public string Client { get; set; }
        [EmailAddress]
        public string Mail { get; set; }
        public string View_user { get; set; }

        // Step 2
        public List<int> SelectedExistingGroupIds { get; set; } = new();

        // Step 2/3
        public List<GroupCreateViewModel> NewGroups { get; set; } = new();
    }

    public class GroupCreateViewModel
    {
        public int Id { get; set; } // Group ID, used for editing existing groups
        public string Title { get; set; }
        public string Comment { get; set; }
        public List<ReportCreateViewModel> Reports { get; set; } = new();
        public List<int> SelectedExistingUserIds { get; set; } = new();
    }

    public class ReportCreateViewModel
    {
        public string Title { get; set; }
        public string Id { get; set; }   // user-specified id (string)
        public int Order { get; set; }
        public string? IdWeb { get; set; }
        public byte[]? Report { get; set; }
    }
}
