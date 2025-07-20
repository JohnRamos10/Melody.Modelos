using Melody.API.Consumer;
using Melody.Modelos;
using Melody.MVC.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Melody.MVC.Controllers
{
    public class ReportesController : Controller
    {
        public async Task<IActionResult> GenerarReportePagos()
        {
            // Obtener pagos desde la API con Crud<T>
            List<Pago> pagos = await Crud<Pago>.GetAll();

            if (pagos == null) pagos = new List<Pago>();

            // Generar el PDF con QuestPDF
            byte[] pdfBytes = ReportePDF.Generar(pagos);

            // Devolver el archivo PDF al navegador para descargar
            return File(pdfBytes, "application/pdf", "reporte-pagos.pdf");
        }
    }
}
