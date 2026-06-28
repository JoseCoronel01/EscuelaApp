namespace EscuelaApp.Models
{
    public class Materia
    {
        public int Id { get; set; }
        public string Clave { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Creditos { get; set; }
        public int HorasSemanales { get; set; }
        public bool Activo { get; set; } = true;
    }
}
