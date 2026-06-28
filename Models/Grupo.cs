namespace EscuelaApp.Models
{
    public class Grupo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty;
        public int CicloEscolar { get; set; }
        public int MaestroId { get; set; }
        public string MaestroNombre { get; set; } = string.Empty;
        public int CapacidadMaxima { get; set; }
        public bool Activo { get; set; } = true;
    }
}
