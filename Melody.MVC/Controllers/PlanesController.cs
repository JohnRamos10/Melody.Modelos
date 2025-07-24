using Melody.API.Consumer;
using Melody.Modelos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Melody.MVC.Controllers
{
    public class PlanesController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var planes = await Crud<Plan>.GetAll();
            return View(planes);
        }

        // Muestra detalles de un plan
        public async Task<IActionResult> Details(int id)
        {
            var plan = Crud<Plan>.GetById(id);
            return View(plan);
        }
    }
}
