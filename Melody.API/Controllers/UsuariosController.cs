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
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<UsuariosController> _logger;
        private readonly BlobContainerClient _perfilesContainer;

        public UsuariosController(AppDbContext context, UserManager<Usuario> userManger, IConfiguration configuration, ILogger<UsuariosController> logger)
        {
            _context = context;
            _userManager = userManger;
            _logger = logger;
            string pefilesSasUrl = configuration["AzureStorage:Perfiles"];
            _perfilesContainer = new BlobContainerClient(new Uri(pefilesSasUrl));
        }

        // Helper method para obtener usuario actual
        private async Task<Usuario?> ObtenerUsuarioActualAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return null;

            return await _userManager.FindByIdAsync(userId.ToString());
        }

        // GET: api/Usuarios
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<Usuario>>> ObtenerUsuarios()
        {
            try
            {
                var usuarios = await _userManager.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Nombre,
                        u.Apellido,
                        u.Email,
                        u.FotoPerfil,
                        u.FechaRegistro,
                        u.EmailConfirmed
                    })
                    .OrderByDescending(u => u.FechaRegistro)
                    .ToListAsync();
                //Agregar roles a cada usuario
                var usuariosConRoles = new List<object>();
                foreach (var usuario in usuarios)
                {
                    var usuarioEntity = await _userManager.FindByIdAsync(usuario.Id.ToString());
                    var roles = await _userManager.GetRolesAsync(usuarioEntity);

                    usuariosConRoles.Add(new
                    {
                        usuario.Id,
                        usuario.Nombre,
                        usuario.Apellido,
                        usuario.Email,
                        usuario.FotoPerfil,
                        usuario.FechaRegistro,
                        usuario.EmailConfirmed,
                        Roles = roles
                    });
                }
                return Ok(usuariosConRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener usuarios");
            }
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Usuario>> ObtenerUsuario(int id)
        {
            try
            {
                var usuarioActual = await ObtenerUsuarioActualAsync();
                if (usuarioActual == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                // Verificar si es admin o el mismo usuario
                var rolesActual = await _userManager.GetRolesAsync(usuarioActual);
                if (!rolesActual.Contains("admin") && usuarioActual.Id != id)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "No tienes permisos para ver este usuario");
                }

                var usuario = await _userManager.FindByIdAsync(id.ToString());
                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado");
                }
                var roles = await _userManager.GetRolesAsync(usuario);
                var suscripciones = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Where(s => s.UsuarioId == id)
                    .OrderByDescending(s => s.FechaInicio)
                    .Take(5)
                    .Select(s => new
                    {
                        s.Id,
                        s.PlanId,
                        PlanNombre = s.Plan.Nombre,
                        s.FechaInicio,
                        s.FechaFin
                    })
                    .ToListAsync();
                var resultado = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Apellido,
                    usuario.Email,
                    usuario.FotoPerfil,
                    usuario.FechaRegistro,
                    usuario.EmailConfirmed,
                    Roles = roles,
                    Suscripciones = suscripciones
                };
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener usuario");
            }
        }

        //GET: api/Usuarios/mi-perfil - Obtiene el perfil del usuario autenticado

        [HttpGet("mi-perfil")]
        [Authorize]
        public async Task<ActionResult<object>> ObtenerMiPerfil()
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var roles = await _userManager.GetRolesAsync(usuario);
                var suscripcionActiva = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Where(s => s.UsuarioId == usuario.Id && s.EsActiva)
                    .FirstOrDefaultAsync();

                var perfil = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Apellido,
                    usuario.Email,
                    usuario.FotoPerfil,
                    usuario.FechaRegistro,
                    usuario.EmailConfirmed,
                    Roles = roles,
                    SuscripcionActiva = suscripcionActiva != null ? new
                    {
                        suscripcionActiva.Id,
                        suscripcionActiva.FechaInicio,
                        suscripcionActiva.FechaFin,
                        PlanNombre = suscripcionActiva.Plan!.Nombre,
                        PlanPrecio = suscripcionActiva.Plan.Precio
                    } : null
                };

                return Ok(perfil);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el perfil del usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el perfil");
            }
        }

        // PUT: api/Usuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarPerfil([FromForm] ActualizarPerfilUsuarioDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var usuarioActual = await ObtenerUsuarioActualAsync();
                if (usuarioActual == null)
                {
                    return Unauthorized();
                }
                //Acutalizar foto de perfil si se proporciona
                if (dto.FotoPerfil != null)
                {
                    var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(dto.FotoPerfil.FileName).ToLower();
                    if (!extensionesPermitidas.Contains(extension))
                    {
                        return BadRequest("Formato de imagen no permitido. Use .jpg, .jpeg o .png");
                    }
                    if (dto.FotoPerfil.Length > 5 * 1024 * 1024) // 2 MB
                    {
                        return BadRequest("La foto no puede ser mayor a 5 MB");
                    }
                    // Eliminar foto anterior si existe
                    if (!string.IsNullOrEmpty(usuarioActual.FotoPerfil))
                    {
                        await EliminarArchivoBlobAsync(usuarioActual.FotoPerfil);
                    }

                    // Subir nueva foto
                    usuarioActual.FotoPerfil = await SubirFotoPerfilAsync(dto.FotoPerfil);
                }
                // Actualizar otros campos
                if (!string.IsNullOrEmpty(dto.Nombre))
                    usuarioActual.Nombre = dto.Nombre;
                if (!string.IsNullOrEmpty(dto.Apellido))
                    usuarioActual.Apellido = dto.Apellido;
                var resultado = await _userManager.UpdateAsync(usuarioActual);
                if (!resultado.Succeeded)
                {
                    return BadRequest(resultado.Errors);
                }
                _logger.LogInformation($"Usuario {usuarioActual.Id} actualizado correctamente");
                return Ok(new
                {
                    mensaje = "Perfil actualizado con éxito",
                    usuario = new
                    {
                        usuarioActual.Id,
                        usuarioActual.Nombre,
                        usuarioActual.Apellido,
                        usuarioActual.Email,
                        usuarioActual.FotoPerfil
                    }
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar perfil de usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar perfil");
            }
        }

        // PUT: api/Usuarios/5/cambiar-rol - Cambiar rol de usuario (solo admin)
        [HttpPut("{id}/cambiar-rol")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CambiarRol(int id, [FromBody] CambiarRolDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var rolesPermitidos = new[] { "userfree", "userpremium", "admin" };
                if (!rolesPermitidos.Contains(dto.NuevoRol))
                {
                    return BadRequest("Rol no válido. Roles permitidos: userfree, userpremium, artista, admin");
                }

                var usuario = await _userManager.FindByIdAsync(id.ToString());
                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                // Obtener roles actuales y removerlos
                var rolesActuales = await _userManager.GetRolesAsync(usuario);
                if (rolesActuales.Any())
                {
                    await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
                }

                // Agregar nuevo rol
                var resultado = await _userManager.AddToRoleAsync(usuario, dto.NuevoRol);

                if (!resultado.Succeeded)
                {
                    _logger.LogError("Error al cambiar rol de usuario: {Errors}",
                        string.Join(", ", resultado.Errors.Select(e => e.Description)));
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error al cambiar el rol");
                }

                _logger.LogInformation("Rol cambiado para usuario {Email}: {NuevoRol}", usuario.Email, dto.NuevoRol);

                return Ok(new
                {
                    mensaje = "Rol actualizado con éxito",
                    usuario = new
                    {
                        usuario.Id,
                        usuario.Email,
                        NuevoRol = dto.NuevoRol
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar rol del usuario con ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al cambiar el rol");
            }
        }
        // GET: api/Usuarios/buscar?q=nombre - Buscar usuarios (solo admin)
        [HttpGet("buscar")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<object>>> BuscarUsuarios([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest("El término de búsqueda es requerido");
                }

                var usuarios = await _userManager.Users
                    .Where(u => u.Nombre.Contains(q) ||
                               u.Apellido.Contains(q) ||
                               u.Email.Contains(q))
                    .Select(u => new
                    {
                        u.Id,
                        u.Nombre,
                        u.Apellido,
                        u.Email,
                        u.FotoPerfil
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar usuarios con término: {SearchTerm}", q);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error en la búsqueda");
            }
        }
        private async Task<string> SubirFotoPerfilAsync(IFormFile foto)
        {
            var extension = Path.GetExtension(foto.FileName).ToLower();
            var nombreFoto = $"usuario_{Guid.NewGuid()}{extension}";
            var blobClient = _perfilesContainer.GetBlobClient(nombreFoto);
            using (var stream = foto.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }
            return $"https://appmelody.blob.core.windows.net/perfiles/{nombreFoto}";
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
                    _logger.LogError(ex, "Error al eliminar archivo de perfil");
                    throw new Exception("Error al eliminar archivo de perfil", ex);
                }
            }
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }
    }
}
