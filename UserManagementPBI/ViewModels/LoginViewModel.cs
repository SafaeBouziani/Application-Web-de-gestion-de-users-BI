using System.ComponentModel.DataAnnotations;

namespace UserManagementPBI.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage ="Votre nom d'utilisateur est obligatoire")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Mot de passe est obligatoire")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }

    }
}
