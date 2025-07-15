using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class ActualizarPerfilArtistaDto
    {
        public string NombreArtista { get; set; } = string.Empty;
        public string? Biografia { get; set; }
        public IFormFile? ImagenPerfil { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public IFormFile? FotoPerfil { get; set; }
    }
}
