using System.Text.Json;

namespace concilliation_consumer;

public class JSONReader
{
    public static List<PaymentCheck> ReadFile(
        string filePath,
        DateTime date,
        long paymentProviderId
        )
    {

        int batchSize = 10;

        List<PaymentCheck> result = [];

        if (File.Exists(filePath))
        {
            using StreamReader fileReader = new(filePath);
            string? line;
            int linecount = 0;
            List<PaymentCheck> currentPayments = [];
            
            while ((line = fileReader.ReadLine()) != null)
            {
                // Console.WriteLine(line);
                PaymentCheck? paymentCheck = JsonSerializer.Deserialize<PaymentCheck>(line);
                // Console.WriteLine("Id: " + paymentCheck.Id);
                // Console.WriteLine("Status: " + paymentCheck.Status);
                linecount++;
                currentPayments.Add(paymentCheck);

                if (linecount >= batchSize)
                {
                    currentPayments.Clear();
                    linecount = 0;
                }
            }
        }
        else
        {
            Console.WriteLine("invalid path");
        }
        return result;
    }
}