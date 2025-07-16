using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Azure.Storage.Blobs;
using Melody.Modelos.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArtistasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<ArtistasController> _logger;
        private readonly BlobContainerClient _perfilesContainer;


        public ArtistasController(AppDbContext context, UserManager<Usuario> userManager, IConfiguration configuration, ILogger<ArtistasController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;

            string perfilesSasUrl = configuration["AzureStorage:Perfiles"];
            _perfilesContainer = new BlobContainerClient(new Uri(perfilesSasUrl));
        }

        // Helper method para obtener usuario actual
        private async Task<Usuario?> ObtenerUsuarioActualAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return null;

            return await _userManager.FindByIdAsync(userId.ToString());
        }

        //Obtener todos los artistas públicos
        // GET: api/Artistas 
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerArtistas()
        {
            try
            {
                var artistas = await _context.Artistas
                    .Include(a => a.Usuario)
                    .Include(a => a.Canciones)
                    .Include(a => a.Albums)
                    .Select(a => new
                    {
                        a.Id,
                        a.NombreArtista,
                        a.Biografia,
                        a.ImagenPerfil,
                        TotalCanciones = a.Canciones != null ? a.Canciones.Count : 0,
                        TotalAlbums = a.Albums != null ? a.Albums.Count : 0,
                        FechaRegistro = a.Usuario!.FechaRegistro
                    })
                    .OrderByDescending(a => a.FechaRegistro)
                    .ToListAsync();
                return Ok(artistas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener artistas");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener artistas");
            }
        }

        // GET: api/Artistas/5 - Obtener artista específico (público)
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> ObtenerArtista(int id)
        {
            try
            {
                // Primero obtenemos los datos básicos del artista
                var artista = await _context.Artistas
                    .Include(a => a.Usuario)
                    .Include(a => a.Seguidores)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (artista == null)
                {
                    return NotFound("Artista no encontrado");
                }

                // Luego obtenemos las canciones por separado
                var canciones = await _context.Canciones
                    .Include(c => c.Genero)
                    .Where(c => c.ArtistaId == id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Titulo,
                        c.FechaLanzamiento,
                        c.PortadaUrl,
                        c.Duracion,
                        GeneroNombre = c.Genero!.Nombre
                    })
                    .OrderByDescending(c => c.FechaLanzamiento)
                    .Take(10)
                    .ToListAsync();

                // Y los álbums por separado
                var albums = await _context.Albums
                    .Where(a => a.ArtistaId == id)
                    .Select(album => new
                    {
                        album.Id,
                        album.Titulo,
                        album.FechaLanzamiento,
                        album.PortadaUrl
                    })
                    .OrderByDescending(album => album.FechaLanzamiento)
                    .ToListAsync();

                // Construimos la respuesta
                var resultado = new
                {
                    artista.Id,
                    artista.NombreArtista,
                    artista.Biografia,
                    artista.ImagenPerfil,
                    FechaRegistro = artista.Usuario!.FechaRegistro,
                    TotalSeguidores = artista.Seguidores?.Count ?? 0,
                    TotalCanciones = canciones.Count,
                    TotalAlbums = albums.Count,
                    Canciones = canciones,
                    Albums = albums
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el artista con ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el artista");
            }
        }


        // GET: api/Artistas/mi-perfil - Obtener perfil del artista autenticado
        [HttpGet("mi-perfil")]
        [Authorize(Roles = "artista")]
        public async Task<ActionResult<object>> ObtenerMiPerfil()
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");

                }
                var artista = await _context.Artistas
                    .Include(a => a.Usuario)
                    .Include(a => a.Canciones)
                    .Include(a => a.Albums)
                    .Include(a => a.Seguidores)
                    .FirstOrDefaultAsync(a => a.UsuarioId == usuario.Id);
                if (artista == null)
                {
                    return NotFound("Perfil de artista no encontrado");
                }
                var perfil = new
                {
                    artista.Id,
                    artista.NombreArtista,
                    artista.Biografia,
                    artista.ImagenPerfil,
                    Usuario = new
                    {
                        usuario.Nombre,
                        usuario.Apellido,
                        usuario.Email,
                        usuario.FotoPerfil,
                        usuario.FechaRegistro
                    },
                    Estadisticas = new
                    {
                        TotalCanciones = artista.Canciones?.Count ?? 0,
                        TotalAlbums = artista.Albums?.Count ?? 0,
                        TotalSeguidores = artista.Seguidores?.Count ?? 0
                    }
                };
                return Ok(perfil);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el perfil del artista");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el perfil");
            }
        }



        // PUT: api/Artistas/actualizar-perfil - Actualizar perfil del artista
        [HttpPut("actualizar-perfil")]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> ActualizarPerfil([FromForm] ActualizarPerfilArtistaDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var artista = await _context.Artistas
                    .FirstOrDefaultAsync(a => a.UsuarioId == usuario.Id);

                if (artista == null)
                {
                    return NotFound("Perfil de artista no encontrado");
                }

                // Actualizar imagen de perfil si se proporciona una nueva
                if (dto.ImagenPerfil != null)
                {
                    var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(dto.ImagenPerfil.FileName).ToLower();

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        return BadRequest("Formato de imagen no permitido. Use .jpg, .jpeg, .png o .webp");
                    }

                    if (dto.ImagenPerfil.Length > 5 * 1024 * 1024) // 5MB
                    {
                        return BadRequest("La imagen no puede exceder los 5 MB");
                    }

                    // Eliminar imagen anterior si existe
                    if (!string.IsNullOrEmpty(artista.ImagenPerfil))
                    {
                        await EliminarArchivoBlobAsync(artista.ImagenPerfil);
                    }

                    // Subir nueva imagen
                    artista.ImagenPerfil = await SubirImagenPerfilAsync(dto.ImagenPerfil);
                }

                // Actualizar datos del artista
                artista.NombreArtista = dto.NombreArtista;
                artista.Biografia = dto.Biografia;

                // Actualizar datos del usuario si se proporcionan
                if (!string.IsNullOrEmpty(dto.Nombre))
                    usuario.Nombre = dto.Nombre;

                if (!string.IsNullOrEmpty(dto.Apellido))
                    usuario.Apellido = dto.Apellido;

                // Actualizar foto de perfil del usuario si se proporciona
                if (dto.FotoPerfil != null)
                {
                    var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(dto.FotoPerfil.FileName).ToLower();

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        return BadRequest("Formato de foto de perfil no permitido. Use .jpg, .jpeg, .png o .webp");
                    }

                    if (dto.FotoPerfil.Length > 5 * 1024 * 1024) // 5MB
                    {
                        return BadRequest("La foto de perfil no puede exceder los 5 MB");
                    }

                    // Eliminar foto anterior si existe
                    if (!string.IsNullOrEmpty(usuario.FotoPerfil))
                    {
                        await EliminarArchivoBlobAsync(usuario.FotoPerfil);
                    }

                    // Subir nueva foto
                    usuario.FotoPerfil = await SubirImagenPerfilAsync(dto.FotoPerfil);
                }

                await _context.SaveChangesAsync();
                var resultadoUsuario = await _userManager.UpdateAsync(usuario);

                if (!resultadoUsuario.Succeeded)
                {
                    _logger.LogError("Error al actualizar usuario: {Errors}",
                        string.Join(", ", resultadoUsuario.Errors.Select(e => e.Description)));
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el usuario");
                }

                _logger.LogInformation("Perfil de artista actualizado: {NombreArtista}", artista.NombreArtista);

                return Ok(new
                {
                    mensaje = "Perfil actualizado con éxito",
                    artista = new
                    {
                        artista.Id,
                        artista.NombreArtista,
                        artista.Biografia,
                        artista.ImagenPerfil
                    },
                    usuario = new
                    {
                        usuario.Nombre,
                        usuario.Apellido,
                        usuario.FotoPerfil
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el perfil del artista");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el perfil");
            }
        }

        // GET: api/Artistas/buscar?q=nombre - Buscar artistas
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<object>>> BuscarArtistas([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest("El término de búsqueda es requerido");
                }

                var artistas = await _context.Artistas
                    .Include(a => a.Usuario)
                    .Where(a => a.NombreArtista.Contains(q) ||
                               (a.Usuario!.Nombre + " " + a.Usuario.Apellido).Contains(q))
                    .Select(a => new
                    {
                        a.Id,
                        a.NombreArtista,
                        a.ImagenPerfil,
                        NombreCompleto = a.Usuario!.Nombre + " " + a.Usuario.Apellido
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(artistas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar artistas con término: {SearchTerm}", q);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error en la búsqueda");
            }
        }
        // Métodos auxiliares para manejo de archivos
        private async Task<string> SubirImagenPerfilAsync(IFormFile imagen)
        {
            var extension = Path.GetExtension(imagen.FileName).ToLower();
            var nombreImagen = $"perfil_{Guid.NewGuid()}{extension}";
            var blobClient = _perfilesContainer.GetBlobClient(nombreImagen);

            using (var stream = imagen.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            return $"https://appmelody.blob.core.windows.net/perfiles/{nombreImagen}";
        }

        private async Task EliminarArchivoBlobAsync(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var uri = new Uri(url);
                    var blobName = uri.Segments.Last();
                    var blobClient = _perfilesContainer.GetBlobClient(blobName);
                    await blobClient.DeleteIfExistsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al eliminar archivo blob: {Url}", url);
                }
            }
        }
    }
}
