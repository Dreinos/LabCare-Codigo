using System.ComponentModel.DataAnnotations;
using LabCare.Models;

namespace LabCare.ViewModels;

public class SolicitudDatos
{
    private string? _solicitante;
    private string? _descripcion;

    [Required(ErrorMessage = "Escribe el nombre del solicitante.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El solicitante debe tener entre 3 y 100 caracteres.")]
    public string? Solicitante
    {
        get => _solicitante;
        set => _solicitante = value?.Trim();
    }

    [Display(Name = "Descripción")]
    [Required(ErrorMessage = "Escribe la descripción de la falla.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres.")]
    public string? Descripcion
    {
        get => _descripcion;
        set => _descripcion = value?.Trim();
    }

    [Required(ErrorMessage = "Selecciona la prioridad.")]
    [RegularExpression("^(BAJA|MEDIA|ALTA)$", ErrorMessage = "La prioridad debe ser BAJA, MEDIA o ALTA.")]
    public string? Prioridad { get; set; }
}

public sealed class SolicitudNueva : SolicitudDatos
{
    [Display(Name = "Equipo")]
    [Required(ErrorMessage = "Selecciona un equipo.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un equipo válido.")]
    public int? EquipoId { get; set; }
}

public sealed record SolicitudesListado(
    SolicitudNueva Nueva,
    IReadOnlyList<Equipo> Equipos,
    IReadOnlyList<Solicitud> Solicitudes,
    string? Busqueda,
    string? Prioridad,
    string? Estado);

public sealed class SolicitudEdicion : SolicitudDatos
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Equipo { get; set; } = "";
}

public sealed record ErrorVista(int Codigo, string Mensaje);
