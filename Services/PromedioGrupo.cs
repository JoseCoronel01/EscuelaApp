namespace EscuelaApp.Services;

public class PromedioGrupo
{
    public string Grupo { get; set; } = "";
    public string Grado { get; set; } = "";
    public int TotalAlumnos { get; set; }
    public decimal Promedio { get; set; }
    public int Aprobados { get; set; }
    public int Reprobados { get; set; }
}
