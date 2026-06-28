using EscuelaApp.Models;

namespace EscuelaApp.Services
{
    public class AlumnoService(DatabaseService db, ExcelExportService excel)
    {
        private readonly ObjectPatternQuery _q = db.Query;

        public async Task<List<Alumno>> ObtenerTodosAsync(string? busqueda = null)
        {
            var opciones = new QueryOptions { OrdenarPor = "ApellidoPaterno" };

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                opciones.Filtros.Add(new QueryFilter
                {
                    Campo = "LOWER(Nombre || ' ' || ApellidoPaterno || ' ' || ApellidoMaterno || ' ' || Matricula)",
                    Operador = "LIKE",
                    Valor = busqueda.ToLower()
                });
            }

            var filas = await _q.EjecutarAsync("Alumnos", opciones);
            return filas.Select(MapearAlumno).ToList();
        }

        public async Task<Alumno?> ObtenerPorIdAsync(int id)
        {
            var opciones = new QueryOptions
            {
                Filtros = [new QueryFilter { Campo = "Id", Operador = "=", Valor = id }]
            };
            var filas = await _q.EjecutarAsync("Alumnos", opciones);
            return filas.Count > 0 ? MapearAlumno(filas[0]) : null;
        }

        public async Task<long> GuardarAsync(Alumno alumno)
        {
            var valores = new Dictionary<string, object?>
            {
                ["Matricula"] = alumno.Matricula,
                ["Nombre"] = alumno.Nombre,
                ["ApellidoPaterno"] = alumno.ApellidoPaterno,
                ["ApellidoMaterno"] = alumno.ApellidoMaterno,
                ["FechaNacimiento"] = alumno.FechaNacimiento.ToString("yyyy-MM-dd"),
                ["Genero"] = alumno.Genero,
                ["Email"] = alumno.Email,
                ["Telefono"] = alumno.Telefono,
                ["Direccion"] = alumno.Direccion,
                ["Activo"] = alumno.Activo ? 1 : 0,
                ["FechaRegistro"] = alumno.FechaRegistro.ToString("o")
            };

            if (alumno.Id == 0)
                return await _q.InsertarAsync("Alumnos", valores);

            await _q.ActualizarAsync("Alumnos", valores, new() { ["Id"] = alumno.Id });
            return alumno.Id;
        }

        public async Task EliminarAsync(int id) =>
            await _q.EliminarAsync("Alumnos", new() { ["Id"] = id });

        public async Task<int> ContarAsync()
        {
            var r = await _q.EscalarAsync("SELECT COUNT(*) FROM Alumnos WHERE Activo = 1");
            return Convert.ToInt32(r);
        }

        public async Task<byte[]> ExportarExcelAsync(string? busqueda = null)
        {
            var lista = await ObtenerTodosAsync(busqueda);
            return excel.ExportarAlumnos(lista);
        }

        private static Alumno MapearAlumno(Dictionary<string, object?> row) => new()
        {
            Id = Convert.ToInt32(row["Id"]),
            Matricula = row["Matricula"]?.ToString() ?? "",
            Nombre = row["Nombre"]?.ToString() ?? "",
            ApellidoPaterno = row["ApellidoPaterno"]?.ToString() ?? "",
            ApellidoMaterno = row["ApellidoMaterno"]?.ToString() ?? "",
            FechaNacimiento = DateTime.TryParse(row["FechaNacimiento"]?.ToString(), out var fn) ? fn : DateTime.MinValue,
            Genero = row["Genero"]?.ToString() ?? "",
            Email = row["Email"]?.ToString() ?? "",
            Telefono = row["Telefono"]?.ToString() ?? "",
            Direccion = row["Direccion"]?.ToString() ?? "",
            Activo = Convert.ToInt32(row["Activo"] ?? 0) == 1,
            FechaRegistro = DateTime.TryParse(row["FechaRegistro"]?.ToString(), out var fr) ? fr : DateTime.Now
        };
    }
}
