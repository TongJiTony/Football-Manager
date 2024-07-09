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
    }

}
