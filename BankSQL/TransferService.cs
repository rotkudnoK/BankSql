using Npgsql;

namespace BankSQL
{
    public class TransferService(string connectionString)
    {
        private readonly string connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        public async Task<long> TransferAsync(long sourceId, long destId, decimal amount, CancellationToken ct = default)
        {
            if (amount <= 0) throw new ArgumentException("Сумма перевода должна быть положительной.");
            if (sourceId == destId) throw new ArgumentException("Аккаунт отправителя должен отличаться от аккаунта получателя.");

            const string sqlScript = @"
            WITH src AS (
                SELECT id, balance, is_active, currency FROM accounts WHERE id = @sourceId FOR UPDATE
            ),
            dst AS (
                SELECT id, currency FROM accounts WHERE id = @destId FOR UPDATE
            ),
            validation AS (
                SELECT 
                    src.id AS src_id, dst.id AS dst_id, src.currency AS src_currency,
                    CASE
                        WHEN NOT src.is_active              THEN 'SOURCE_NOT_ACTIVE'
                        WHEN src.currency <> dst.currency   THEN 'CURRENCY_MISMATCH'
                        WHEN src.balance < @amount          THEN 'INSUFFICIENT_BALANCE'
                        ELSE NULL
                    END AS error_code
                FROM src CROSS JOIN dst
            ),
            upd_src AS (
                UPDATE accounts
                SET balance = balance - @amount
                WHERE id = (SELECT src_id FROM validation WHERE error_code IS NULL)),
            upd_dst AS (
                UPDATE accounts
                SET balance = balance + @amount
                WHERE id = (SELECT dst_id FROM validation WHERE error_code IS NULL)),
            ins AS (
                INSERT INTO transfers (from_account_id, to_account_id, amount, currency, status)
                SELECT src_id, dst_id, @amount, src_currency,
                       CASE WHEN error_code IS NULL THEN 'completed' ELSE 'rejected' END
                FROM validation
                RETURNING id
            )
            SELECT ins.id, validation.error_code FROM ins, validation;";

            await using var dbConnection = new NpgsqlConnection(connectionString);
            await dbConnection.OpenAsync(ct);
            await using var transaction = await dbConnection.BeginTransactionAsync(ct);

            await using var command = new NpgsqlCommand(sqlScript, dbConnection, transaction);
            command.Parameters.AddWithValue("@sourceId", sourceId);
            command.Parameters.AddWithValue("@destId", destId);
            command.Parameters.AddWithValue("@amount", amount);

            long transferId;
            await using (var reader = await command.ExecuteReaderAsync(ct))
            {
                await reader.ReadAsync(ct);
                transferId = Convert.ToInt64(reader["id"]);
            }

            await transaction.CommitAsync(ct);
            return Convert.ToInt64(transferId);
        }
    }
}