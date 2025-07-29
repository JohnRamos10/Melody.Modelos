using Melody.API.Consumer;
using Melody.Modelos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Melody.MVC.Controllers
{
    public class SuscripcionesController : Controller
    {
        // GET: SuscripcionesController/Create?planId=...
        public async Task<IActionResult> Create(int planId)
        {
            var plan = await Crud<Plan>.GetById(planId);
            ViewBag.Plan = plan;

            var nuevaSuscripcion = new Suscripcion
            {
                PlanId = planId
            };

            return View(nuevaSuscripcion);
        }

        // POST: Suscripciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Suscripcion suscripcion)
        {
            try
            {
                var plan = await Crud<Plan>.GetById(suscripcion.PlanId);

                suscripcion.FechaInicio = DateTime.Now;
                suscripcion.FechaFin = DateTime.Now.AddDays(plan.DuracionDias);
                suscripcion.EsActiva = plan.Precio == 0;

                // Obtener UsuarioId desde la sesión
                suscripcion.UsuarioId = Convert.ToInt32(HttpContext.Session.GetString("UsuarioId"));

                // Crear la suscripción en la API
                var suscripcionCreada = await Crud<Suscripcion>.Create(suscripcion);

                if (plan.Precio == 0)
                {
                    // Si es plan gratuito, redirigir directamente
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // Si es plan premium, redirigir a la vista de pago
                    return RedirectToAction("Create", "Pagos", new { suscripcionId = suscripcionCreada.Id });
                }
            }
            catch
            {
                return View(suscripcion);
            }
        }
    }
}
