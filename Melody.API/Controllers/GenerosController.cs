using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Authorization;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenerosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GenerosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Generos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Genero>>> ObtenerGeneros()
        {
            var generos = await _context.Generos
                .Select(g => new
                {
                    g.Id,
                    g.Nombre,
                    TotalAlbums = g.Albums != null ? g.Albums.Count : 0,
                    TotalCanciones = g.Canciones != null ? g.Canciones.Count : 0
                })
                .OrderBy(g => g.Nombre)
                .ToListAsync();
            return Ok(generos);
        }

        // GET: api/Generos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Genero>> ObtenerGenero(int id)
        {
            var genero = await _context.Generos.FindAsync(id);

            if (genero == null)
            {
                return NotFound();
            }
            // Cargar las relaciones de álbumes y canciones
            var albumes = await _context.Albums
                .Include(a => a.Artista)
                .Where(a => a.GeneroId == id)
                .Select(a => new
                {
                    a.Id,
                    a.Titulo,
                    a.FechaLanzamiento,
                    a.PortadaUrl,
                    NombreArtista = a.Artista!.NombreArtista
                })
                .OrderByDescending(a => a.FechaLanzamiento)
                .Take(10).ToListAsync();
            var canciones = await _context.Canciones
                .Include(c => c.Artista)
                .Where(c => c.GeneroId == id)
                .Select(c => new
                {
                    c.Id,
                    c.Titulo,
                    c.FechaLanzamiento,
                    NombreArtista = c.Artista!.NombreArtista
                })
                .OrderByDescending(c => c.FechaLanzamiento)
                .Take(10).ToListAsync();
            var resultado = new
            {
                genero.Id,
                genero.Nombre,
                TotalAlbums = albumes.Count,
                TotalCanciones = canciones.Count,
                Albums = albumes,
                Canciones = canciones
            };

            return Ok(resultado);
        }

        // PUT: api/Generos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ActualizarGenero(int id, Genero genero)
        {
            if (id != genero.Id)
            {
                return BadRequest();
            }
            // Verificar si el género existe
            var existingGenero = await _context.Generos.FindAsync(id);
            if (existingGenero == null)
            {
                return NotFound();
            }
            //Verificar si el nombre del género ya existe
            var nombreExistente = await _context.Generos
                .FirstOrDefaultAsync(g => g.Nombre.ToLower() == genero.Nombre.ToLower() && g.Id != id);
            if (nombreExistente != null)
                return BadRequest("Ya existe un género con el mismo nombre.");

            _context.Entry(genero).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GeneroExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Generos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<Genero>> CrearGenero(Genero genero)
        {
            // Verificar si el nombre del género ya existe
            var nombreExistente = await _context.Generos
                .FirstOrDefaultAsync(g => g.Nombre.ToLower() == genero.Nombre.ToLower());
            if (nombreExistente != null)
                return BadRequest("Ya existe un género con el mismo nombre.");
            _context.Generos.Add(genero);
            await _context.SaveChangesAsync();

            return CreatedAtAction("ObtenerGenero", new { id = genero.Id }, genero);
        }

        // DELETE: api/Generos/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EliminarGenero(int id)
        {
            try
            {
                var genero = await _context.Generos
                    .Include(g => g.Albums)
                    .Include(g => g.Canciones)
                    .FirstOrDefaultAsync(g => g.Id == id);
                if (genero == null)
                {
                    return NotFound();
                }
                //Verificar si el género tiene álbumes o canciones
                if ((genero.Albums != null && genero.Albums.Count > 0) || (genero.Canciones != null && genero.Canciones.Count > 0))
                {
                    return BadRequest("No se puede eliminar el género porque tiene álbumes o canciones asociados.");
                }
                _context.Generos.Remove(genero);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error al eliminar el género: {ex.Message}");
            }
        }
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<object>>> BuscarGeneros([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest("El término de búsqueda es requerido");
                }

                var generos = await _context.Generos
                    .Where(g => g.Nombre.Contains(q))
                    .Select(g => new
                    {
                        g.Id,
                        g.Nombre,
                        TotalAlbums = g.Albums != null ? g.Albums.Count : 0,
                        TotalCanciones = g.Canciones != null ? g.Canciones.Count : 0
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(generos);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error en la búsqueda");
            }
        }

        // GET: api/Generos/populares - Obtener géneros más populares (público)
        [HttpGet("populares")]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerGenerosPopulares()
        {
            try
            {
                var generos = await _context.Generos
                    .Include(g => g.Albums)
                    .Include(g => g.Canciones)
                    .ToListAsync();

                var generosPopulares = generos
                    .Select(g => new
                    {
                        g.Id,
                        g.Nombre,
                        TotalContenido = (g.Albums?.Count ?? 0) + (g.Canciones?.Count ?? 0)
                    })
                    .Where(g => g.TotalContenido > 0)
                    .OrderByDescending(g => g.TotalContenido)
                    .Take(10)
                    .ToList();

                return Ok(generosPopulares);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener géneros populares");
            }
        }


        private bool GeneroExists(int id)
        {
            return _context.Generos.Any(e => e.Id == id);
        }
    }
}
