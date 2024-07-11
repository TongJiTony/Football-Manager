namespace FootballManagerBackend
{
    using Microsoft.Extensions.Configuration;
    using Oracle.ManagedDataAccess.Client;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;

    public class OracleDbContext
    {
        private readonly string _connectionString;

        public OracleDbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var results = new List<Dictionary<string, object>>();

            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            }

                            results.Add(row);
                        }
                    }
                }
            }

            return results;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, IDictionary<string, object> parameters = null)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(new OracleParameter(param.Key, param.Value));
                        }
                    }

                    var result = new List<Dictionary<string, object>>();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var dict = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                dict.Add(reader.GetName(i), reader.GetValue(i));
                            }

                            result.Add(dict);
                        }
                    }

                    return result;
                }
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string query, IDictionary<string, object> parameters = null)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            try
                            {
                                command.Parameters.Add(new OracleParameter(param.Key, param.Value ?? DBNull.Value));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error creating parameter {param.Key}: {ex.Message}");
                                throw;
                            }
                        }
                    }

                    try
                    {
                        return await command.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error executing query: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryWithParametersAsync(string query, List<OracleParameter> parameters)
        {
            var results = new List<Dictionary<string, object>>();

            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            }

                            results.Add(row);
                        }
                    }
                }
            }

            return results;
        }

        public async Task ExecuteNonQueryAsyncForAdd(string query, Dictionary<string, object> parameters = null, OracleParameter outParameter = null)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new OracleCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(new OracleParameter(param.Key, param.Value));
                        }
                    }

                    if (outParameter != null)
                    {
                        command.Parameters.Add(outParameter);
                    }

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

    }
}
