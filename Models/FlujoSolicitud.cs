namespace LabCare.Models;

public static class FlujoSolicitud
{
    public const string Pendiente = "PENDIENTE";
    public const string EnProceso = "EN_PROCESO";
    public const string Resuelta = "RESUELTA";
    public const string Cancelada = "CANCELADA";
    public const string EquipoOperativo = "OPERATIVO";

    public static IReadOnlyList<string> Prioridades { get; } = ["BAJA", "MEDIA", "ALTA"];
    public static IReadOnlyList<string> Estados { get; } = [Pendiente, EnProceso, Resuelta, Cancelada];

    public static IReadOnlyList<string> Destinos(string estado) => estado switch
    {
        Pendiente => [EnProceso, Cancelada],
        EnProceso => [Resuelta, Cancelada],
        _ => []
    };
}
