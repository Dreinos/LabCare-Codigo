namespace LabCare.Models;

public class Solicitud
{
    public int Id { get; set; }
    public string CodigoSolicitud { get; set; } = "";
    public int EquipoId { get; set; }
    public Equipo Equipo { get; set; } = null!;
    public string Solicitante { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Prioridad { get; set; } = "";
    public string Estado { get; set; } = FlujoSolicitud.Pendiente;
    public DateTime FechaRegistro { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public bool Activo { get; set; } = true;
}
