using EscuelaApp.Models;

namespace EscuelaApp.Services
{
    public class GrupoService(DatabaseService db, ExcelExportService excel)
    {
        private readonly ObjectPatternQuery _q = db.Query;

        public async Task<List<Grupo>> ObtenerTodosAsync(string? busqueda = null)
        {
            var sql = @"
            SELECT g.*, (m.Nombre || ' ' || m.ApellidoPaterno) AS MaestroNombre
            FROM Grupos g
            LEFT JOIN Maestros m ON g.MaestroId = m.Id
            ORDER BY g.Nombre";

            var filas = await _q.QueryAsync(sql);

            if (!string.IsNullOrWhiteSpace(busqueda))
                filas = filas.Where(f =>
                    (f["Nombre"]?.ToString() ?? "").Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                    (f["Grado"]?.ToString() ?? "").Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                ).ToList();

            return filas.Select(MapearGrupo).ToList();
        }

        public async Task<Grupo?> ObtenerPorIdAsync(int id)
        {
            var sql = @"SELECT g.*, (m.Nombre || ' ' || m.ApellidoPaterno) AS MaestroNombre
                    FROM Grupos g
                    LEFT JOIN Maestros m ON g.MaestroId = m.Id
                    WHERE g.Id = @id";
            var filas = await _q.QueryAsync(sql, new() { ["@id"] = id });
            return filas.Count > 0 ? MapearGrupo(filas[0]) : null;
        }

        public async Task<long> GuardarAsync(Grupo grupo)
        {
            var valores = new Dictionary<string, object?>
            {
                ["Nombre"] = grupo.Nombre,
                ["Grado"] = grupo.Grado,
                ["Turno"] = grupo.Turno,
                ["CicloEscolar"] = grupo.CicloEscolar,
                ["MaestroId"] = grupo.MaestroId,
                ["CapacidadMaxima"] = grupo.CapacidadMaxima,
                ["Activo"] = grupo.Activo ? 1 : 0
            };

            if (grupo.Id == 0) return await _q.InsertarAsync("Grupos", valores);
            await _q.ActualizarAsync("Grupos", valores, new() { ["Id"] = grupo.Id });
            return grupo.Id;
        }

        public async Task EliminarAsync(int id) =>
            await _q.EliminarAsync("Grupos", new() { ["Id"] = id });

        public async Task<byte[]> ExportarExcelAsync(string? busqueda = null)
        {
            var lista = await ObtenerTodosAsync(busqueda);
            return excel.ExportarGrupos(lista);
        }

        private static Grupo MapearGrupo(Dictionary<string, object?> row) => new()
        {
            Id = Convert.ToInt32(row["Id"]),
            Nombre = row["Nombre"]?.ToString() ?? "",
            Grado = row["Grado"]?.ToString() ?? "",
            Turno = row["Turno"]?.ToString() ?? "",
            CicloEscolar = Convert.ToInt32(row["CicloEscolar"] ?? 0),
            MaestroId = Convert.ToInt32(row["MaestroId"] ?? 0),
            MaestroNombre = row.ContainsKey("MaestroNombre") ? row["MaestroNombre"]?.ToString() ?? "" : "",
            CapacidadMaxima = Convert.ToInt32(row["CapacidadMaxima"] ?? 30),
            Activo = Convert.ToInt32(row["Activo"] ?? 0) == 1
        };
    }
}
