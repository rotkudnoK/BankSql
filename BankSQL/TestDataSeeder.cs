using Npgsql;

namespace BankSQL
{
    public class TestDataSeeder(string connectionString, string sqlScriptPath, string logFilePath = "seeder_errors.log")
    {
        readonly string connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        readonly string scriptFilePath = sqlScriptPath ?? throw new ArgumentNullException(nameof(sqlScriptPath));
        readonly string logFilePath = logFilePath;

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            string sqlScript;
            try
            {
                sqlScript = await File.ReadAllTextAsync(scriptFilePath, cancellationToken);
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"Не удалось прочитать SQL-файл '{scriptFilePath}': {ex.Message}");
                throw;
            }

            await using var dbConnection = new NpgsqlConnection(connectionString);
            await dbConnection.OpenAsync(cancellationToken);
            await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);

            try
            {
                await using var command = new NpgsqlCommand(sqlScript, dbConnection, transaction);
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                if (transaction.Connection != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                await LogErrorAsync($"Ошибка выполнения транзакции инициализации данных. Выполнен ROLLBACK. Детали: {ex.Message}");
                throw;
            }
        }

        private async Task LogErrorAsync(string errorMessage)
        {
            var logEntry = $"[{DateTime.UtcNow}] {errorMessage}{Environment.NewLine}";
            await File.AppendAllTextAsync(logFilePath, logEntry);
        }
    }
}
