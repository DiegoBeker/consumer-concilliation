using Npgsql;

namespace concilliation_consumer;

public class DatabaseHandler
{
    private readonly string _connectionString;

    public DatabaseHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> Count(DateTime date, long paymentProviderId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var countDb = 0;

        var sql = @"
            SELECT Count(*)
            FROM ""Payment"" payment
            INNER JOIN ""PaymentProviderAccount"" AS origin
                ON origin.""Id"" = payment.""PaymentProviderAccountId""
            INNER JOIN ""PixKey"" AS pix
                ON pix.""Id"" = payment.""PixKeyId""
            INNER JOIN ""PaymentProviderAccount"" AS destiny
                ON destiny.""Id"" = pix.""PaymentProviderAccountId""
            WHERE date_trunc('day', payment.""CreatedAt"") = @date AND (
                origin.""PaymentProviderId"" = @paymentProviderId OR
                destiny.""PaymentProviderId"" = @paymentProviderId)";

        using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("date", date);
        cmd.Parameters.AddWithValue("paymentProviderId", paymentProviderId);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var count = reader.GetInt32(0);
            countDb = count;
        }

        return countDb;
    }

    public async Task<List<PaymentCheck>> Retrieve(
        DateTime date,
        long paymentProviderId,
        int batchCount,
        int batchSize
    )
    {
        var dbPayments = new List<PaymentCheck>();

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
            SELECT payment.""Id"", payment.""Status""
            FROM ""Payment"" payment
            INNER JOIN ""PaymentProviderAccount"" AS origin
                ON origin.""Id"" = payment.""PaymentProviderAccountId""
            INNER JOIN ""PixKey"" AS pix
                ON pix.""Id"" = payment.""PixKeyId""
            INNER JOIN ""PaymentProviderAccount"" AS destiny
                ON destiny.""Id"" = pix.""PaymentProviderAccountId""
            WHERE date_trunc('day', payment.""CreatedAt"") = @date AND (
                origin.""PaymentProviderId"" = @paymentProviderId OR
                destiny.""PaymentProviderId"" = @paymentProviderId)
            ORDER BY payment.""Id""
            OFFSET @batchCount * @batchSize
            LIMIT @batchSize";

            using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("date", date);
            cmd.Parameters.AddWithValue("paymentProviderId", paymentProviderId);
            cmd.Parameters.AddWithValue("batchCount", batchCount);
            cmd.Parameters.AddWithValue("batchSize", batchSize);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetInt64(0);
                var status = reader.GetInt32(1);
                string? statusText = Enum.GetName(typeof(PaymentStatus), status);

                var paymentCheck = new PaymentCheck(id, statusText);
                dbPayments.Add(paymentCheck);
            }

            return dbPayments;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao recuperar dados do banco de dados: {ex.Message}");
            return dbPayments; // Retorna a lista vazia em caso de erro
        }
    }
}
