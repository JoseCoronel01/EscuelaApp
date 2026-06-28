using EscuelaApp.Models;
using Microsoft.Data.Sqlite;
using System.Text;

namespace EscuelaApp.Services
{
    public class ObjectPatternQuery
    {
        private readonly string _connectionString;

        public ObjectPatternQuery(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Dictionary<string, object?>>> EjecutarAsync(
            string tabla, QueryOptions opciones)
        {
            var sql = new StringBuilder();

            var campos = opciones.Campos.Count > 0
                ? string.Join(", ", opciones.Campos)
                : "*";
            sql.Append($"SELECT {campos} FROM {tabla}");

            var parametros = new Dictionary<string, object?>();
            if (opciones.Filtros.Count > 0)
            {
                var condiciones = new List<string>();
                int i = 0;
                foreach (var f in opciones.Filtros)
                {
                    var paramName = $"@p{i}";
                    var op = f.Operador?.ToUpper() ?? "=";

                    if (op == "LIKE")
                    {
                        condiciones.Add($"{f.Campo} LIKE {paramName}");
                        parametros[paramName] = $"%{f.Valor}%";
                    }
                    else if (op == "IN" && f.Valor is IEnumerable<object> lista)
                    {
                        var inParams = lista.Select((_, idx) => $"@in{idx}").ToList();
                        condiciones.Add($"{f.Campo} IN ({string.Join(",", inParams)})");
                        var vals = lista.ToList();
                        for (int j = 0; j < vals.Count; j++)
                            parametros[$"@in{j}"] = vals[j];
                    }
                    else
                    {
                        condiciones.Add($"{f.Campo} {op} {paramName}");
                        parametros[paramName] = f.Valor;
                    }
                    i++;
                }
                sql.Append(" WHERE " + string.Join(" AND ", condiciones));
            }

            if (!string.IsNullOrEmpty(opciones.OrdenarPor))
            {
                sql.Append($" ORDER BY {opciones.OrdenarPor}");
                if (opciones.Descendente) sql.Append(" DESC");
            }

            if (opciones.Limite.HasValue)
            {
                sql.Append($" LIMIT {opciones.Limite}");
                if (opciones.Offset.HasValue)
                    sql.Append($" OFFSET {opciones.Offset}");
            }

            return await QueryAsync(sql.ToString(), parametros);
        }

        public async Task<List<Dictionary<string, object?>>> QueryAsync(
            string sql, Dictionary<string, object?>? parametros = null)
        {
            var resultados = new List<Dictionary<string, object?>>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            AgregarParametros(cmd, parametros);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var fila = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                    fila[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                resultados.Add(fila);
            }
            return resultados;
        }

        public async Task<int> EjecutarComandoAsync(
            string sql, Dictionary<string, object?>? parametros = null)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            AgregarParametros(cmd, parametros);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<long> InsertarAsync(
            string tabla, Dictionary<string, object?> valores)
        {
            var campos = string.Join(", ", valores.Keys);
            var paramNames = string.Join(", ", valores.Keys.Select(k => $"@{k}"));
            var sql = $"INSERT INTO {tabla} ({campos}) VALUES ({paramNames}); SELECT last_insert_rowid();";

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var kv in valores)
                cmd.Parameters.AddWithValue($"@{kv.Key}", kv.Value ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return result is long id ? id : 0;
        }

        public async Task<int> ActualizarAsync(
            string tabla,
            Dictionary<string, object?> valores,
            Dictionary<string, object?> condicion)
        {
            var sets = string.Join(", ", valores.Keys.Select(k => $"{k} = @set_{k}"));
            var where = string.Join(" AND ", condicion.Keys.Select(k => $"{k} = @wh_{k}"));
            var sql = $"UPDATE {tabla} SET {sets} WHERE {where}";

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var kv in valores)
                cmd.Parameters.AddWithValue($"@set_{kv.Key}", kv.Value ?? DBNull.Value);
            foreach (var kv in condicion)
                cmd.Parameters.AddWithValue($"@wh_{kv.Key}", kv.Value ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> EliminarAsync(
            string tabla, Dictionary<string, object?> condicion)
        {
            var where = string.Join(" AND ", condicion.Keys.Select(k => $"{k} = @{k}"));
            var sql = $"DELETE FROM {tabla} WHERE {where}";
            return await EjecutarComandoAsync(sql, condicion.ToDictionary(
                kv => $"@{kv.Key}", kv => kv.Value));
        }

        public async Task<object?> EscalarAsync(
            string sql, Dictionary<string, object?>? parametros = null)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            AgregarParametros(cmd, parametros);
            return await cmd.ExecuteScalarAsync();
        }

        private static void AgregarParametros(
            SqliteCommand cmd, Dictionary<string, object?>? parametros)
        {
            if (parametros == null) return;
            foreach (var kv in parametros)
                cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);
        }
    }
}
