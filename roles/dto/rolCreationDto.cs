using System.ComponentModel.DataAnnotations;

namespace project.roles.dto
{
    public class rolCreationDto
    {
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [StringLength(50, ErrorMessage = "El campo {0} no puede tener mas de {1} caracteres")]
        public string name { get; set; } = string.Empty;
    }
}
