using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos;
using Melody.API.Consumer;
using Microsoft.AspNetCore.Mvc.Rendering;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Melody.MVC.Controllers
{
    public class CancionesController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var data = await Crud<Cancion>.GetAll();
            return View(data);
        }

        public async Task<IActionResult> Details(int id)
        {
            var data = await Crud<Cancion>.GetById(id);
            return View(data);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Generos = await GetGeneros();
            return View();
        }

        private async Task<List<SelectListItem>> GetGeneros()
        {
            var generos = await Crud<Genero>.GetAll();
            return generos.Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Nombre
            }).ToList();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cancion data)
        {
            try
            {
                await Crud<Cancion>.Create(data);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Generos = await GetGeneros();
                return View(data);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var data = await Crud<Cancion>.GetById(id);
            ViewBag.Generos = await GetGeneros();
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cancion data)
        {
            try
            {
                await Crud<Cancion>.Update(id, data);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Generos = await GetGeneros();
                return View(data);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var data = await Crud<Cancion>.GetById(id);
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, Cancion data)
        {
            try
            {
                await Crud<Cancion>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(data);
            }
        }
    }

}
