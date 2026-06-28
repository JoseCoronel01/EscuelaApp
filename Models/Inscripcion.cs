namespace EscuelaApp.Models
{
    public class Inscripcion
    {
        public int Id { get; set; }
        public int AlumnoId { get; set; }
        public string AlumnoNombre { get; set; } = string.Empty;
        public string AlumnoMatricula { get; set; } = string.Empty;
        public int GrupoId { get; set; }
        public string GrupoNombre { get; set; } = string.Empty;
        public int MateriaId { get; set; }
        public string MateriaNombre { get; set; } = string.Empty;
        public DateTime FechaInscripcion { get; set; } = DateTime.Now;
        public string Estado { get; set; } = "Activa";  // Activa, Baja, Finalizada
        public string CicloEscolar { get; set; } = string.Empty;
    }
}
