using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace project.users.dto
{
    public class clientDtoBase
    {
        [Required(ErrorMessage = "El campo Nombre es requerido")]
        [StringLength(100, ErrorMessage = "El campo Nombre no puede tener más de 100 caracteres")]
        public string name { get; set; } = null!;

        [Required(ErrorMessage = "El campo Fecha de Nacimiento es requerido")]
        public DateOnly birthdate { get; set; }

        [Required(ErrorMessage = "El campo Correo es requerido")]
        [EmailAddress]
        public string email { get; set; } = null!;

        [Required(ErrorMessage = "El campo Teléfono es requerido")]
        [StringLength(10, ErrorMessage = "El campo Teléfono no puede tener más de 10 caracteres")]
        public string tel { get; set; } = null!;

        [Required(ErrorMessage = "El campo Dirección es requerido")]
        [StringLength(100, ErrorMessage = "El campo Dirección no puede tener más de 100 caracteres")]
        public string address { get; set; } = null!;
    }
}
