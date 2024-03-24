using Npgsql;

namespace concilliation_consumer;

public class DatabaseHandler
{
    private readonly string _connectionString;

    public DatabaseHandler(string connectionString)
    {
        _connectionString = connectionString;
    }



    public async Task<List<PaymentCheck>> RetrieveDataFromDatabase(
        DateTime date,
        long paymentProviderId,
        List<PaymentCheck> filePayments
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
                destiny.""PaymentProviderId"" = @paymentProviderId
            )";
            using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("date", date);
            cmd.Parameters.AddWithValue("paymentProviderId", paymentProviderId);

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
