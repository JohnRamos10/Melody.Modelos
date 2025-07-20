using Melody.API.Consumer;
using Melody.Modelos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Melody.MVC.Controllers
{
    public class PagosController : Controller
    {
        // GET: Pagos  
        public  async Task<IActionResult> Index()
        {
            List<Pago> pagos = await Crud<Pago>.GetAll();
            return View(pagos);
        }
    }
}
