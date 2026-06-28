namespace EscuelaApp.Models
{
    public class QueryFilter
    {
        public string? Campo { get; set; }
        public string? Operador { get; set; }  // =, LIKE, >, <, >=, <=, IN
        public object? Valor { get; set; }
    }
}
