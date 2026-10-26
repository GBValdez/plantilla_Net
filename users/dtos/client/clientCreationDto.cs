using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace project.users.dto
{
    public class clientCreationDto : clientDtoBase
    {
        [Required, StringLength(64), RegularExpression("^[a-zA-Z0-9]+$")]
        public string userName { get; set; } = string.Empty;
        [Required(ErrorMessage = "El campo Contraseña es requerido")]
        [MinLength(8)]
        public string password { get; set; } = string.Empty;

    }
}
