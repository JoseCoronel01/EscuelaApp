namespace EscuelaApp.Models
{
    public class QueryOptions
    {
        public List<string> Campos { get; set; } = new();           // SELECT
        public List<QueryFilter> Filtros { get; set; } = new();     // WHERE
        public string? OrdenarPor { get; set; }                     // ORDER BY
        public bool Descendente { get; set; } = false;
        public int? Limite { get; set; }                            // LIMIT
        public int? Offset { get; set; }                            // OFFSET
    }
}
