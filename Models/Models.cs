namespace EscuelaApp.Models;

// ─── Catálogos ────────────────────────────────────────────────────────────────

public class Alumno
{
    public int Id { get; set; }
    public string Matricula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Genero { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public string NombreCompleto => $"{Nombre} {ApellidoPaterno} {ApellidoMaterno}".Trim();
}

public class Maestro
{
    public int Id { get; set; }
    public string NumeroEmpleado { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public string NombreCompleto => $"{Nombre} {ApellidoPaterno} {ApellidoMaterno}".Trim();
}

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

// ─── Movimientos ─────────────────────────────────────────────────────────────

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

// ─── Query Pattern ────────────────────────────────────────────────────────────

public class QueryFilter
{
    public string? Campo { get; set; }
    public string? Operador { get; set; }  // =, LIKE, >, <, >=, <=, IN
    public object? Valor { get; set; }
}

public class QueryOptions
{
    public List<string> Campos { get; set; } = new();           // SELECT
    public List<QueryFilter> Filtros { get; set; } = new();     // WHERE
    public string? OrdenarPor { get; set; }                     // ORDER BY
    public bool Descendente { get; set; } = false;
    public int? Limite { get; set; }                            // LIMIT
    public int? Offset { get; set; }                            // OFFSET
}
