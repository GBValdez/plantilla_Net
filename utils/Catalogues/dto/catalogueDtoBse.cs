using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace project.utils.Catalogues.dto
{
    public class catalogueDtoBse
    {
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [StringLength(50, ErrorMessage = "El campo {0} no puede tener mas de {1} caracteres")]
        public string name { get; set; } = string.Empty;
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [StringLength(255, ErrorMessage = "El campo {0} no puede tener mas de {1} caracteres")]
        public string description { get; set; } = string.Empty;
    }
}
