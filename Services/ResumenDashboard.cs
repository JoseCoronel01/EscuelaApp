namespace EscuelaApp.Services
{
    public class ResumenDashboard
    {
        public int TotalAlumnos { get; set; }
        public int TotalMaestros { get; set; }
        public int TotalMaterias { get; set; }
        public int TotalInscripciones { get; set; }
        public decimal PromedioGeneral { get; set; }
        public int Aprobados { get; set; }
        public int Reprobados { get; set; }
    }
}
