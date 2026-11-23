using LabCare.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LabCare.Controllers;

public class ErrorController : Controller
{
    [Route("error")]
    [IgnoreAntiforgeryToken]
    public IActionResult Mostrar(int? codigo)
    {
        if (HttpContext.Features.Get<IExceptionHandlerPathFeature>() is not null)
        {
            codigo = 500;
        }

        var error = codigo is >= 400 and < 600 ? codigo.Value : 404;
        Response.StatusCode = error;
        var mensaje = error switch
        {
            404 => "No se encontró la página o la solicitud indicada.",
            400 => "La petición no es válida. Vuelve al listado y repite la operación.",
            _ => "No se pudo completar la operación. Inténtalo de nuevo."
        };
        return View("Error", new ErrorVista(error, mensaje));
    }
}
