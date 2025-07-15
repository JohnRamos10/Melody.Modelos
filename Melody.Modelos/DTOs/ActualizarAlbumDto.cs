using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class ActualizarAlbumDto
    {
        public string? Titulo { get; set; }
        public int GeneroId { get; set; }
        public IFormFile? Portada { get; set; }
    }
}
