using System.ComponentModel.DataAnnotations;

namespace UserManagementPBI.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Votre nom d'utilisateur est obligatoire")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "Mot de passe est obligatoire")]
        [DataType(DataType.Password)]
        [StringLength(40,MinimumLength =8)]
        [Display(Name ="Nouveau mot de passe")]
        public string NewPassword { get; set; }
        [Required(ErrorMessage = "Confirmez votre Mot de passe!")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        public string ConfirmPassword { get; set; }
    }
}
