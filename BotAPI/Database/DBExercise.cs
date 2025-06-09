using Npgsql;
using BotAPI.Models;
using BotAPI;

namespace BotAPI.Database
{
    public class DBExercise
    {
        private readonly NpgsqlConnection _connection;
        public DBExercise(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Postgres");
            _connection = new NpgsqlConnection(connectionString);
        }

    }
}
