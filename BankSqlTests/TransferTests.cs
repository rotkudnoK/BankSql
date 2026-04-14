using BankSQL;
using Npgsql;

namespace BankSqlTests
{
    public class TransferTests : IAsyncLifetime
    {
        readonly static string connectionString = "Host=localhost;Port=5499;Database=postgres;Username=postgres;Password=1234";
        readonly static string sqlScriptFilePath = "seed-test-data.sql";
        TestDataSeeder testDataSeeder = new(connectionString, sqlScriptFilePath);

        public async Task InitializeAsync()
        {
            await DbCleanupAsync();
            await testDataSeeder.SeedAsync();
        }

        [Fact]
        public async Task TotalBalancePreservationTest ()
        {
            TransferService transferService = new(connectionString);
            var totalBalanceBefore = await GetTotalBalanceAsync();
            var accountIds = await GetAccountIdsAsync();

            for (int i = 0; i < 10; i++)
            {
                long sourceId = accountIds[Random.Shared.Next(accountIds.Count)];
                long destId;
                do
                {
                    destId = accountIds[Random.Shared.Next(accountIds.Count)];
                } while (sourceId == destId);
                var amount = Random.Shared.Next(1000, 100000) / 100m;
                
                await transferService.TransferAsync(sourceId, destId, amount);
            }
            var totalBalanceAfter = await GetTotalBalanceAsync();

            Assert.Equal(totalBalanceBefore, totalBalanceAfter);
        }

        private async Task<List<long>> GetAccountIdsAsync()
        {
            await using var dbConnection = new NpgsqlConnection(connectionString);
            await dbConnection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT id FROM accounts;", dbConnection);
            
            var list = new List<long>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(reader.GetInt64(0));
            }
            return list;
        }

        private async Task<decimal> GetTotalBalanceAsync()
        {
            await using var dbConnection = new NpgsqlConnection(connectionString);
            await dbConnection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT SUM(balance) FROM accounts;", dbConnection);
            return (decimal)await command.ExecuteScalarAsync();
        }

        private async Task DbCleanupAsync()
        {
            await using var dbConnection = new NpgsqlConnection(connectionString);
            await dbConnection.OpenAsync();
            await using var command = new NpgsqlCommand(@"
                DELETE FROM transfers;
                DELETE FROM accounts;
                DELETE FROM clients;", dbConnection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DisposeAsync() => await DbCleanupAsync();
    }
}