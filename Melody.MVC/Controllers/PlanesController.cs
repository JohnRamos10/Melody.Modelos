using Melody.API.Consumer;
using Melody.Modelos;
using Melody.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Melody.MVC.Controllers
{
    [Authorize(Roles = "admin")]
    public class PlanesController : Controller
    {
        private readonly AuthService _authService;

        public PlanesController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: Planes
        public async Task<IActionResult> Index()
        {
            var planes = await Crud<Plan>.GetAll();
            return View(planes);
        }

        // GET: Planes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var plan = await Crud<Plan>.GetById(id);
            if (plan == null) return NotFound();
            return View(plan);
        }

        // GET: Planes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Planes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Plan plan)
        {
            if (!ModelState.IsValid) return View(plan);

            try
            {
                await Crud<Plan>.Create(plan);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Error al crear el plan.");
                return View(plan);
            }
        }

        // GET: Planes/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var plan = await Crud<Plan>.GetById(id);
            if (plan == null) return NotFound();
            return View(plan);
        }

        // POST: Planes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Plan plan)
        {
            if (id != plan.Id) return BadRequest();
            if (!ModelState.IsValid) return View(plan);

            try
            {
                await Crud<Plan>.Update(id, plan);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Error al actualizar el plan.");
                return View(plan);
            }
        }

        // GET: Planes/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var plan = await Crud<Plan>.GetById(id);
            if (plan == null) return NotFound();
            return View(plan);
        }

        // POST: Planes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await Crud<Plan>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Error al eliminar el plan.");
                var plan = await Crud<Plan>.GetById(id);
                return View(plan);
            }
        }
    }
}
