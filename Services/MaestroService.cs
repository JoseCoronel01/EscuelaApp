using EscuelaApp.Models;

namespace EscuelaApp.Services
{
    public class MaestroService(DatabaseService db, ExcelExportService excel)
    {
        private readonly ObjectPatternQuery _q = db.Query;

        public async Task<List<Maestro>> ObtenerTodosAsync(string? busqueda = null)
        {
            var opciones = new QueryOptions { OrdenarPor = "ApellidoPaterno" };

            if (!string.IsNullOrWhiteSpace(busqueda))
                opciones.Filtros.Add(new QueryFilter
                {
                    Campo = "LOWER(Nombre || ' ' || ApellidoPaterno || ' ' || NumeroEmpleado)",
                    Operador = "LIKE",
                    Valor = busqueda.ToLower()
                });

            var filas = await _q.EjecutarAsync("Maestros", opciones);
            return filas.Select(MapearMaestro).ToList();
        }

        public async Task<Maestro?> ObtenerPorIdAsync(int id)
        {
            var filas = await _q.EjecutarAsync("Maestros", new QueryOptions
            {
                Filtros = [new QueryFilter { Campo = "Id", Operador = "=", Valor = id }]
            });
            return filas.Count > 0 ? MapearMaestro(filas[0]) : null;
        }

        public async Task<long> GuardarAsync(Maestro maestro)
        {
            var valores = new Dictionary<string, object?>
            {
                ["NumeroEmpleado"] = maestro.NumeroEmpleado,
                ["Nombre"] = maestro.Nombre,
                ["ApellidoPaterno"] = maestro.ApellidoPaterno,
                ["ApellidoMaterno"] = maestro.ApellidoMaterno,
                ["Especialidad"] = maestro.Especialidad,
                ["Email"] = maestro.Email,
                ["Telefono"] = maestro.Telefono,
                ["Activo"] = maestro.Activo ? 1 : 0,
                ["FechaRegistro"] = maestro.FechaRegistro.ToString("o")
            };

            if (maestro.Id == 0) return await _q.InsertarAsync("Maestros", valores);
            await _q.ActualizarAsync("Maestros", valores, new() { ["Id"] = maestro.Id });
            return maestro.Id;
        }

        public async Task EliminarAsync(int id) =>
            await _q.EliminarAsync("Maestros", new() { ["Id"] = id });

        public async Task<int> ContarAsync()
        {
            var r = await _q.EscalarAsync("SELECT COUNT(*) FROM Maestros WHERE Activo = 1");
            return Convert.ToInt32(r);
        }

        public async Task<byte[]> ExportarExcelAsync(string? busqueda = null)
        {
            var lista = await ObtenerTodosAsync(busqueda);
            return excel.ExportarMaestros(lista);
        }

        private static Maestro MapearMaestro(Dictionary<string, object?> row) => new()
        {
            Id = Convert.ToInt32(row["Id"]),
            NumeroEmpleado = row["NumeroEmpleado"]?.ToString() ?? "",
            Nombre = row["Nombre"]?.ToString() ?? "",
            ApellidoPaterno = row["ApellidoPaterno"]?.ToString() ?? "",
            ApellidoMaterno = row["ApellidoMaterno"]?.ToString() ?? "",
            Especialidad = row["Especialidad"]?.ToString() ?? "",
            Email = row["Email"]?.ToString() ?? "",
            Telefono = row["Telefono"]?.ToString() ?? "",
            Activo = Convert.ToInt32(row["Activo"] ?? 0) == 1,
            FechaRegistro = DateTime.TryParse(row["FechaRegistro"]?.ToString(), out var fr) ? fr : DateTime.Now
        };
    }
}
