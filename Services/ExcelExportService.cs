using ClosedXML.Excel;
using EscuelaApp.Models;

namespace EscuelaApp.Services;

public class ExcelExportService
{
    private static readonly XLColor ColorEncabezado   = XLColor.FromHtml("#1E3A5F");
    private static readonly XLColor ColorTextoClaro   = XLColor.White;
    private static readonly XLColor ColorFilaAlterna  = XLColor.FromHtml("#EBF2FF");
    private static readonly XLColor ColorTitulo       = XLColor.FromHtml("#0D2137");
    private static readonly XLColor ColorAprobado     = XLColor.FromHtml("#D4EDDA");
    private static readonly XLColor ColorReprobado    = XLColor.FromHtml("#F8D7DA");
    private static readonly XLColor ColorEnCurso      = XLColor.FromHtml("#FFF3CD");

    public byte[] ExportarAlumnos(List<Alumno> alumnos)
    {
        var columnas = new[] {
            "Matrícula","Nombre","Apellido Paterno","Apellido Materno",
            "Fecha Nac.","Género","Email","Teléfono","Dirección","Activo","Registro"
        };
        var filas = alumnos.Select(a => new object?[] {
            a.Matricula, a.Nombre, a.ApellidoPaterno, a.ApellidoMaterno,
            a.FechaNacimiento.ToString("dd/MM/yyyy"), a.Genero,
            a.Email, a.Telefono, a.Direccion,
            a.Activo ? "Sí" : "No",
            a.FechaRegistro.ToString("dd/MM/yyyy HH:mm")
        }).ToList();

        return Generar("Alumnos", "Catálogo de Alumnos", columnas, filas);
    }

    public byte[] ExportarMaestros(List<Maestro> maestros)
    {
        var columnas = new[] {
            "Núm. Empleado","Nombre","Apellido Paterno","Apellido Materno",
            "Especialidad","Email","Teléfono","Activo","Registro"
        };
        var filas = maestros.Select(m => new object?[] {
            m.NumeroEmpleado, m.Nombre, m.ApellidoPaterno, m.ApellidoMaterno,
            m.Especialidad, m.Email, m.Telefono,
            m.Activo ? "Sí" : "No",
            m.FechaRegistro.ToString("dd/MM/yyyy HH:mm")
        }).ToList();

        return Generar("Maestros", "Catálogo de Maestros", columnas, filas);
    }

    public byte[] ExportarMaterias(List<Materia> materias)
    {
        var columnas = new[] {
            "Clave","Nombre","Descripción","Créditos","Horas Semanales","Activo"
        };
        var filas = materias.Select(m => new object?[] {
            m.Clave, m.Nombre, m.Descripcion,
            m.Creditos, m.HorasSemanales,
            m.Activo ? "Sí" : "No"
        }).ToList();

        return Generar("Materias", "Catálogo de Materias", columnas, filas);
    }

    public byte[] ExportarGrupos(List<Grupo> grupos)
    {
        var columnas = new[] {
            "Nombre","Grado","Turno","Ciclo Escolar","Maestro Titular","Capacidad","Activo"
        };
        var filas = grupos.Select(g => new object?[] {
            g.Nombre, g.Grado, g.Turno, g.CicloEscolar,
            g.MaestroNombre, g.CapacidadMaxima,
            g.Activo ? "Sí" : "No"
        }).ToList();

        return Generar("Grupos", "Catálogo de Grupos", columnas, filas);
    }

    public byte[] ExportarInscripciones(List<Inscripcion> inscripciones)
    {
        var columnas = new[] {
            "Matrícula","Alumno","Grupo","Materia",
            "Fecha Inscripción","Ciclo Escolar","Estado"
        };
        var filas = inscripciones.Select(i => new object?[] {
            i.AlumnoMatricula, i.AlumnoNombre, i.GrupoNombre, i.MateriaNombre,
            i.FechaInscripcion.ToString("dd/MM/yyyy HH:mm"),
            i.CicloEscolar, i.Estado
        }).ToList();

        return Generar("Inscripciones", "Movimientos de Inscripciones", columnas, filas,
            estadoColumna: 6, colorMap: new Dictionary<string, XLColor> {
                ["Activa"]     = ColorAprobado,
                ["Baja"]       = ColorReprobado,
                ["Finalizada"] = ColorEnCurso
            });
    }

    public byte[] ExportarCalificaciones(List<Calificacion> calificaciones)
    {
        var columnas = new[] {
            "Alumno","Materia","Grupo","Parcial 1","Parcial 2","Parcial 3",
            "Promedio","Período","Estado","Observaciones"
        };
        var filas = calificaciones.Select(c => new object?[] {
            c.AlumnoNombre, c.MateriaNombre, c.GrupoNombre,
            c.Parcial1, c.Parcial2, c.Parcial3, c.Promedio,
            c.Periodo, c.Estado, c.Observaciones
        }).ToList();

        return Generar("Calificaciones", "Movimientos de Calificaciones", columnas, filas,
            estadoColumna: 8, colorMap: new Dictionary<string, XLColor> {
                ["Aprobado"]  = ColorAprobado,
                ["Reprobado"] = ColorReprobado,
                ["En Curso"]  = ColorEnCurso
            },
            promedioColumna: 6);
    }

    public byte[] ExportarBoleta(string nombreAlumno, List<Calificacion> calificaciones)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Boleta");

        EscribirTitulo(ws, "BOLETA DE CALIFICACIONES", 1, 10);
        ws.Cell(2, 1).Value = $"Alumno: {nombreAlumno}";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 12;
        ws.Range(2, 1, 2, 10).Merge();

        ws.Cell(3, 1).Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Range(3, 1, 3, 10).Merge();

        string[] headers = { "Materia", "Grupo", "Parcial 1", "Parcial 2", "Parcial 3", "Promedio", "Estado" };
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(5, c + 1);
            cell.Value = headers[c];
            EstiloEncabezado(cell);
        }

        for (int r = 0; r < calificaciones.Count; r++)
        {
            var cal = calificaciones[r];
            var fila = r + 6;
            ws.Cell(fila, 1).Value = cal.MateriaNombre;
            ws.Cell(fila, 2).Value = cal.GrupoNombre;
            ws.Cell(fila, 3).Value = cal.Parcial1;
            ws.Cell(fila, 4).Value = cal.Parcial2;
            ws.Cell(fila, 5).Value = cal.Parcial3;
            ws.Cell(fila, 6).Value = cal.Promedio;
            ws.Cell(fila, 7).Value = cal.Estado;

            var color = cal.Estado switch {
                "Aprobado"  => ColorAprobado,
                "Reprobado" => ColorReprobado,
                _           => ColorEnCurso
            };
            ws.Range(fila, 1, fila, 7).Style.Fill.BackgroundColor = color;

            if (cal.Promedio >= 6)
                ws.Cell(fila, 6).Style.Font.FontColor = XLColor.DarkGreen;
            else
                ws.Cell(fila, 6).Style.Font.FontColor = XLColor.DarkRed;
            ws.Cell(fila, 6).Style.Font.Bold = true;
        }

        int ultimaFila = calificaciones.Count + 6;
        ws.Cell(ultimaFila, 1).Value = "PROMEDIO GENERAL";
        ws.Cell(ultimaFila, 1).Style.Font.Bold = true;
        ws.Cell(ultimaFila, 6).FormulaA1 = $"=AVERAGE(F6:F{ultimaFila - 1})";
        ws.Cell(ultimaFila, 6).Style.Font.Bold = true;
        ws.Range(ultimaFila, 1, ultimaFila, 7).Style.Fill.BackgroundColor = ColorEncabezado;
        ws.Range(ultimaFila, 1, ultimaFila, 7).Style.Font.FontColor = ColorTextoClaro;

        AjustarColumnas(ws);
        ws.Columns(1, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        return ToBytes(wb);
    }

    public byte[] ExportarKardex(
        List<Alumno> alumnos,
        List<Calificacion> calificaciones,
        List<Inscripcion> inscripciones)
    {
        using var wb = new XLWorkbook();

        var wsResumen = wb.Worksheets.Add("Resumen");
        EscribirTitulo(wsResumen, "KARDEX ESCOLAR — RESUMEN", 1, 6);
        string[] hResumen = { "Alumno", "Matrícula", "Materias Inscritas", "Aprobadas", "Reprobadas", "Promedio Gral." };
        for (int c = 0; c < hResumen.Length; c++)
        {
            EstiloEncabezado(wsResumen.Cell(3, c + 1));
            wsResumen.Cell(3, c + 1).Value = hResumen[c];
        }

        var agrupados = calificaciones.GroupBy(c => c.AlumnoId).ToList();
        for (int r = 0; r < alumnos.Count; r++)
        {
            var alumno = alumnos[r];
            var cals = agrupados.FirstOrDefault(g => g.Key == alumno.Id)?.ToList() ?? new();
            var inscs = inscripciones.Count(i => i.AlumnoId == alumno.Id);
            var aprob = cals.Count(c => c.Estado == "Aprobado");
            var repro = cals.Count(c => c.Estado == "Reprobado");
            var prom = cals.Count > 0 ? cals.Average(c => (double)c.Promedio) : 0.0;

            wsResumen.Cell(r + 4, 1).Value = alumno.NombreCompleto;
            wsResumen.Cell(r + 4, 2).Value = alumno.Matricula;
            wsResumen.Cell(r + 4, 3).Value = inscs;
            wsResumen.Cell(r + 4, 4).Value = aprob;
            wsResumen.Cell(r + 4, 5).Value = repro;
            wsResumen.Cell(r + 4, 6).Value = Math.Round(prom, 2);

            if (r % 2 == 0)
                wsResumen.Range(r + 4, 1, r + 4, 6).Style.Fill.BackgroundColor = ColorFilaAlterna;
        }
        AjustarColumnas(wsResumen);

        var wsDetalle = wb.Worksheets.Add("Calificaciones");
        EscribirTitulo(wsDetalle, "DETALLE DE CALIFICACIONES", 1, 9);
        string[] hDet = { "Alumno", "Matrícula", "Materia", "Grupo", "P1", "P2", "P3", "Promedio", "Estado" };
        for (int c = 0; c < hDet.Length; c++)
        {
            EstiloEncabezado(wsDetalle.Cell(3, c + 1));
            wsDetalle.Cell(3, c + 1).Value = hDet[c];
        }

        var dictAlumnos = alumnos.ToDictionary(a => a.Id);
        for (int r = 0; r < calificaciones.Count; r++)
        {
            var cal = calificaciones[r];
            var fila = r + 4;
            wsDetalle.Cell(fila, 1).Value = cal.AlumnoNombre;
            wsDetalle.Cell(fila, 2).Value = dictAlumnos.TryGetValue(cal.AlumnoId, out var al) ? al.Matricula : "";
            wsDetalle.Cell(fila, 3).Value = cal.MateriaNombre;
            wsDetalle.Cell(fila, 4).Value = cal.GrupoNombre;
            wsDetalle.Cell(fila, 5).Value = cal.Parcial1;
            wsDetalle.Cell(fila, 6).Value = cal.Parcial2;
            wsDetalle.Cell(fila, 7).Value = cal.Parcial3;
            wsDetalle.Cell(fila, 8).Value = cal.Promedio;
            wsDetalle.Cell(fila, 9).Value = cal.Estado;

            var bg = cal.Estado switch {
                "Aprobado"  => ColorAprobado,
                "Reprobado" => ColorReprobado,
                _           => (r % 2 == 0 ? ColorFilaAlterna : XLColor.White)
            };
            wsDetalle.Range(fila, 1, fila, 9).Style.Fill.BackgroundColor = bg;
        }
        AjustarColumnas(wsDetalle);

        return ToBytes(wb);
    }

    private byte[] Generar(
        string nombreHoja,
        string titulo,
        string[] columnas,
        List<object?[]> filas,
        int? estadoColumna = null,
        Dictionary<string, XLColor>? colorMap = null,
        int? promedioColumna = null)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(nombreHoja);

        EscribirTitulo(ws, titulo, 1, columnas.Length);
        ws.Cell(2, 1).Value = $"Exportado: {DateTime.Now:dd/MM/yyyy HH:mm} | Total: {filas.Count} registros";
        ws.Range(2, 1, 2, columnas.Length).Merge();
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Font.FontSize = 10;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        for (int c = 0; c < columnas.Length; c++)
        {
            var cell = ws.Cell(4, c + 1);
            cell.Value = columnas[c];
            EstiloEncabezado(cell);
        }

        for (int r = 0; r < filas.Count; r++)
        {
            var fila = filas[r];
            var row = r + 5;
            for (int c = 0; c < fila.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = fila[c]?.ToString() ?? "";

                if (promedioColumna.HasValue && c == promedioColumna.Value - 1
                    && decimal.TryParse(fila[c]?.ToString(), out var prom))
                {
                    cell.Style.Font.FontColor = prom >= 6 ? XLColor.DarkGreen : XLColor.DarkRed;
                    cell.Style.Font.Bold = true;
                }
            }

            if (estadoColumna.HasValue && colorMap != null && fila.Length >= estadoColumna.Value)
            {
                var estado = fila[estadoColumna.Value - 1]?.ToString() ?? "";
                if (colorMap.TryGetValue(estado, out var color))
                    ws.Range(row, 1, row, columnas.Length).Style.Fill.BackgroundColor = color;
                else if (r % 2 == 0)
                    ws.Range(row, 1, row, columnas.Length).Style.Fill.BackgroundColor = ColorFilaAlterna;
            }
            else if (r % 2 == 0)
            {
                ws.Range(row, 1, row, columnas.Length).Style.Fill.BackgroundColor = ColorFilaAlterna;
            }
        }

        var rango = ws.Range(4, 1, filas.Count + 5, columnas.Length);
        rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        rango.Style.Border.InsideBorder  = XLBorderStyleValues.Hair;

        AjustarColumnas(ws);
        return ToBytes(wb);
    }

    private static void EscribirTitulo(IXLWorksheet ws, string texto, int fila, int columnas)
    {
        var range = ws.Range(fila, 1, fila, columnas);
        range.Merge();
        var cell = ws.Cell(fila, 1);
        cell.Value = texto;
        cell.Style.Font.Bold        = true;
        cell.Style.Font.FontSize    = 16;
        cell.Style.Font.FontColor   = ColorTextoClaro;
        cell.Style.Fill.BackgroundColor = ColorTitulo;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
        ws.Row(fila).Height = 28;
    }

    private static void EstiloEncabezado(IXLCell cell)
    {
        cell.Style.Font.Bold             = true;
        cell.Style.Font.FontColor        = ColorTextoClaro;
        cell.Style.Fill.BackgroundColor  = ColorEncabezado;
        cell.Style.Alignment.Horizontal  = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.WrapText    = true;
        cell.Style.Border.OutsideBorder  = XLBorderStyleValues.Thin;
    }

    private static void AjustarColumnas(IXLWorksheet ws)
    {
        ws.Columns().AdjustToContents();
        foreach (var col in ws.ColumnsUsed())
            if (col.Width > 45) col.Width = 45;
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
