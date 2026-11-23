using System.Globalization;
using LabCare.Data;
using LabCare.Models;
using LabCare.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace LabCare.Controllers;

[Route("solicitudes")]
public class SolicitudesController(LabCareContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string? prioridad, string? estado)
    {
        return View(await PrepararListado(new SolicitudNueva(), q, prioridad, estado));
    }

    [HttpPost("crear")]
    public async Task<IActionResult> Crear([Bind(Prefix = "Nueva")] SolicitudNueva nueva)
    {
        if (nueva.EquipoId is int equipoId &&
            !await db.Equipos.AnyAsync(e => e.Id == equipoId && e.Estado == FlujoSolicitud.EquipoOperativo))
        {
            ModelState.AddModelError("Nueva.EquipoId", "El equipo seleccionado no está disponible.");
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            ModelState.AddModelError("", "Revisa los campos marcados para registrar la solicitud.");
            return View(nameof(Index), await PrepararListado(nueva));
        }

        var ahora = ObtenerFechaUtc();
        var solicitud = new Solicitud
        {
            EquipoId = nueva.EquipoId!.Value,
            Solicitante = nueva.Solicitante!,
            Descripcion = nueva.Descripcion!,
            Prioridad = nueva.Prioridad!,
            FechaRegistro = ahora,
            FechaActualizacion = ahora
        };

        db.Solicitudes.Add(solicitud);
        for (var intento = 0; ; intento++)
        {
            var sufijo = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            solicitud.CodigoSolicitud = $"SOL-{ahora.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{sufijo}";

            try
            {
                await db.SaveChangesAsync();
                break;
            }
            catch (DbUpdateException ex) when (intento < 2 && ex.InnerException is MySqlException { Number: 1062 })
            {
                // Otra solicitud obtuvo el mismo código. El índice único protege el registro.
            }
        }

        TempData["Exito"] = $"Se registró la solicitud {solicitud.CodigoSolicitud}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle([FromRoute] int id)
    {
        var solicitud = await ConsultarActivas().SingleOrDefaultAsync(s => s.Id == id);
        return solicitud is null ? NotFound() : View(solicitud);
    }

    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Editar([FromRoute] int id)
    {
        var solicitud = await ConsultarActivas().SingleOrDefaultAsync(s => s.Id == id);
        return solicitud is null ? NotFound() : View(new SolicitudEdicion
        {
            Id = solicitud.Id,
            Codigo = solicitud.CodigoSolicitud,
            Equipo = solicitud.Equipo.Nombre,
            Solicitante = solicitud.Solicitante,
            Descripcion = solicitud.Descripcion,
            Prioridad = solicitud.Prioridad
        });
    }

    [HttpPost("{id:int}/editar")]
    public async Task<IActionResult> Editar([FromRoute] int id, SolicitudEdicion edicion)
    {
        var solicitud = await db.Solicitudes.Include(s => s.Equipo)
            .SingleOrDefaultAsync(s => s.Id == id && s.Activo);
        if (solicitud is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            edicion.Id = solicitud.Id;
            edicion.Codigo = solicitud.CodigoSolicitud;
            edicion.Equipo = solicitud.Equipo.Nombre;
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(edicion);
        }

        solicitud.Solicitante = edicion.Solicitante!;
        solicitud.Descripcion = edicion.Descripcion!;
        solicitud.Prioridad = edicion.Prioridad!;
        return await GuardarCambio(solicitud, "Se guardaron los cambios.", nameof(Detalle));
    }

    [HttpPost("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado([FromRoute] int id, string? destino)
    {
        var solicitud = await db.Solicitudes.Include(s => s.Equipo)
            .SingleOrDefaultAsync(s => s.Id == id && s.Activo);
        if (solicitud is null)
        {
            return NotFound();
        }

        if (destino is null || !FlujoSolicitud.Destinos(solicitud.Estado).Contains(destino))
        {
            ModelState.AddModelError("", "La transición solicitada no está permitida desde el estado actual.");
            Response.StatusCode = StatusCodes.Status409Conflict;
            return View(nameof(Detalle), solicitud);
        }

        solicitud.Estado = destino;
        return await GuardarCambio(solicitud, "Se actualizó el estado.", nameof(Detalle));
    }

    [HttpGet("{id:int}/eliminar")]
    public async Task<IActionResult> Eliminar([FromRoute] int id)
    {
        var solicitud = await ConsultarActivas().SingleOrDefaultAsync(s => s.Id == id);
        return solicitud is null ? NotFound() : View(solicitud);
    }

    [HttpPost("{id:int}/eliminar")]
    [ActionName(nameof(Eliminar))]
    public async Task<IActionResult> ConfirmarEliminacion([FromRoute] int id)
    {
        var solicitud = await db.Solicitudes.SingleOrDefaultAsync(s => s.Id == id && s.Activo);
        if (solicitud is null)
        {
            return NotFound();
        }

        solicitud.Activo = false;
        return await GuardarCambio(solicitud, "La solicitud se retiró del listado.", nameof(Index));
    }

    private IQueryable<Solicitud> ConsultarActivas() =>
        db.Solicitudes.AsNoTracking().Include(s => s.Equipo).Where(s => s.Activo);

    private async Task<SolicitudesListado> PrepararListado(
        SolicitudNueva nueva, string? q = null, string? prioridad = null, string? estado = null)
    {
        var consulta = ConsultarActivas();
        q = q?.Trim();

        if (!string.IsNullOrWhiteSpace(q))
        {
            consulta = consulta.Where(s => s.CodigoSolicitud.Contains(q) ||
                s.Solicitante.Contains(q) || s.Equipo.Nombre.Contains(q));
        }

        if (prioridad is not null && FlujoSolicitud.Prioridades.Contains(prioridad))
        {
            consulta = consulta.Where(s => s.Prioridad == prioridad);
        }

        if (estado is not null && FlujoSolicitud.Estados.Contains(estado))
        {
            consulta = consulta.Where(s => s.Estado == estado);
        }

        var equipos = await db.Equipos.AsNoTracking()
            .Where(e => e.Estado == FlujoSolicitud.EquipoOperativo)
            .OrderBy(e => e.Codigo).ToListAsync();
        var solicitudes = await consulta.OrderByDescending(s => s.FechaRegistro)
            .ThenByDescending(s => s.Id).ToListAsync();

        return new(nueva, equipos, solicitudes, q, prioridad, estado);
    }

    private async Task<IActionResult> GuardarCambio(Solicitud solicitud, string mensaje, string destino)
    {
        var ahora = ObtenerFechaUtc();
        solicitud.FechaActualizacion = ahora > solicitud.FechaActualizacion
            ? ahora
            : solicitud.FechaActualizacion.AddTicks(10);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Aviso"] = "La solicitud cambió mientras se procesaba tu acción. Revisa sus datos y vuelve a intentarlo.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] = mensaje;
        return destino == nameof(Index)
            ? RedirectToAction(destino)
            : RedirectToAction(destino, new { id = solicitud.Id });
    }

    private static DateTime ObtenerFechaUtc()
    {
        var ahora = DateTime.UtcNow;
        return ahora.AddTicks(-(ahora.Ticks % 10));
    }
}
