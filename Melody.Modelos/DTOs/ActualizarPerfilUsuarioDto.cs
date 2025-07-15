using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class ActualizarPerfilUsuarioDto
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public IFormFile? FotoPerfil { get; set; }
    }
}
