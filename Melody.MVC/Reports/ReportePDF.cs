using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Melody.Modelos;
namespace Melody.MVC.Reports
{
    public class ReportePDF
    {
        public static byte[] Generar(IEnumerable<Pago> pagos)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    // Encabezado del documento
                    page.Header()
                        .Text("Reporte de Pagos")
                        .FontSize(18)
                        .SemiBold()
                        .FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    // Tabla de contenido
                    page.Content().Table(table =>
                    {
                        // Definición de columnas
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1); // ID
                            columns.RelativeColumn(2); // Monto
                            columns.RelativeColumn(2); // Fecha
                            columns.RelativeColumn(2); // Método
                        });

                        // Encabezado de la tabla
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("ID");
                            header.Cell().Element(CellStyle).Text("Monto");
                            header.Cell().Element(CellStyle).Text("Fecha");
                            header.Cell().Element(CellStyle).Text("Método de Pago");
                        });

                        // Filas de datos
                        foreach (var pago in pagos)
                        {
                            table.Cell().Element(CellStyle).Text(pago.Id.ToString());
                            table.Cell().Element(CellStyle).Text($"${pago.Monto:F2}");
                            table.Cell().Element(CellStyle).Text(pago.FechaPago.ToString("dd/MM/yyyy"));
                            table.Cell().Element(CellStyle).Text(pago.MetodoPago ?? "N/A");
                        }

                        // Estilo reutilizable para las celdas
                        static IContainer CellStyle(IContainer container) =>
                            container.Padding(5)
                                     .BorderBottom(1)
                                     .BorderColor(Colors.Grey.Lighten2);
                    });

                    // Pie de página
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Página ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                });
            }).GeneratePdf();
        }
    }
}
