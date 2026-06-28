using EscuelaApp.Models;

namespace EscuelaApp.Services
{
    public class MateriaService(DatabaseService db, ExcelExportService excel)
    {
        private readonly ObjectPatternQuery _q = db.Query;

        public async Task<List<Materia>> ObtenerTodosAsync(string? busqueda = null)
        {
            var opciones = new QueryOptions { OrdenarPor = "Nombre" };

            if (!string.IsNullOrWhiteSpace(busqueda))
                opciones.Filtros.Add(new QueryFilter
                {
                    Campo = "LOWER(Nombre || ' ' || Clave)",
                    Operador = "LIKE",
                    Valor = busqueda.ToLower()
                });

            var filas = await _q.EjecutarAsync("Materias", opciones);
            return filas.Select(MapearMateria).ToList();
        }

        public async Task<Materia?> ObtenerPorIdAsync(int id)
        {
            var filas = await _q.EjecutarAsync("Materias", new QueryOptions
            {
                Filtros = [new QueryFilter { Campo = "Id", Operador = "=", Valor = id }]
            });
            return filas.Count > 0 ? MapearMateria(filas[0]) : null;
        }

        public async Task<long> GuardarAsync(Materia materia)
        {
            var valores = new Dictionary<string, object?>
            {
                ["Clave"] = materia.Clave,
                ["Nombre"] = materia.Nombre,
                ["Descripcion"] = materia.Descripcion,
                ["Creditos"] = materia.Creditos,
                ["HorasSemanales"] = materia.HorasSemanales,
                ["Activo"] = materia.Activo ? 1 : 0
            };

            if (materia.Id == 0) return await _q.InsertarAsync("Materias", valores);
            await _q.ActualizarAsync("Materias", valores, new() { ["Id"] = materia.Id });
            return materia.Id;
        }

        public async Task EliminarAsync(int id) =>
            await _q.EliminarAsync("Materias", new() { ["Id"] = id });

        public async Task<byte[]> ExportarExcelAsync(string? busqueda = null)
        {
            var lista = await ObtenerTodosAsync(busqueda);
            return excel.ExportarMaterias(lista);
        }

        private static Materia MapearMateria(Dictionary<string, object?> row) => new()
        {
            Id = Convert.ToInt32(row["Id"]),
            Clave = row["Clave"]?.ToString() ?? "",
            Nombre = row["Nombre"]?.ToString() ?? "",
            Descripcion = row["Descripcion"]?.ToString() ?? "",
            Creditos = Convert.ToInt32(row["Creditos"] ?? 0),
            HorasSemanales = Convert.ToInt32(row["HorasSemanales"] ?? 0),
            Activo = Convert.ToInt32(row["Activo"] ?? 0) == 1
        };
    }
}
