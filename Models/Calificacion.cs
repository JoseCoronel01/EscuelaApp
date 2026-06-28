namespace EscuelaApp.Models
{
    public class Calificacion
    {
        public int Id { get; set; }
        public int InscripcionId { get; set; }
        public int AlumnoId { get; set; }
        public string AlumnoNombre { get; set; } = string.Empty;
        public int MateriaId { get; set; }
        public string MateriaNombre { get; set; } = string.Empty;
        public int GrupoId { get; set; }
        public string GrupoNombre { get; set; } = string.Empty;
        public decimal Parcial1 { get; set; }
        public decimal Parcial2 { get; set; }
        public decimal Parcial3 { get; set; }
        public decimal Promedio { get; set; }
        public string Estado { get; set; } = "Aprobado";  // Aprobado, Reprobado, En Curso
        public string Periodo { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}
