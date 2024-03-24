using System.IO;
using System.Linq;
using System.Text.Json;
using concilliation_consumer.Domain;

namespace concilliation_consumer
{
    public class JSONReader
    {
        public static async Task<Transactions> ReadFile(
            string filePath,
            DateTime date,
            long paymentProviderId
        )
        {
            var connString = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=me-faz-um-pix";
            var databaseHandler = new DatabaseHandler(connString);
            int batchSize = 10;

            Transactions transactions = new();

            int dbCount = await databaseHandler.Count(date, paymentProviderId);
            Console.WriteLine($"dbCount: {dbCount}");

            if (File.Exists(filePath))
            {
                List<PaymentCheck> fileData = new List<PaymentCheck>();
                List<PaymentCheck> dbData = new List<PaymentCheck>();

                using StreamReader fileReader = new StreamReader(filePath);
                string? line;
                int linecount = 0;
                int batchCount = 0;

                while ((line = await fileReader.ReadLineAsync()) != null)
                {
                    if (line != null)
                    {
                        PaymentCheck paymentCheck = JsonSerializer.Deserialize<PaymentCheck>(line);
                        fileData.Add(paymentCheck);
                    }

                    linecount++;

                    if (linecount >= batchSize)
                    {
                        dbData = await databaseHandler.Retrieve(date, paymentProviderId, batchCount, batchSize);

                        foreach (var dbPayment in dbData)
                        {
                            var foundInFile = fileData.FirstOrDefault(d => d.Id == dbPayment.Id);

                            if (foundInFile == null)
                            {
                                transactions.DataBaseToFile.Add(dbPayment);
                            }
                            else if (dbPayment.Status != foundInFile.Status)
                            {
                                transactions.DifferentStatus.Add(dbPayment);
                                fileData.Remove(foundInFile);
                            }
                            else
                            {
                                fileData.Remove(foundInFile);
                            }
                        }

                        // Update dbCount
                        dbCount -= batchSize;

                        if (dbCount > 0)
                        {
                            batchCount++;
                            dbData = await databaseHandler.Retrieve(date, paymentProviderId, batchCount, batchSize);
                        }

                        transactions.FileToDatabase.AddRange(fileData.Where(d => dbData.All(db => db.Id != d.Id)));

                        fileData.Clear();
                        linecount = 0;
                    }
                }

                while (dbCount >= 0)
                {
                    dbData = await databaseHandler.Retrieve(date, paymentProviderId, batchCount, batchSize);
                    transactions.DataBaseToFile.AddRange(dbData);
                    dbCount -= batchSize;
                    batchCount++;
                }
            }
            else
            {
                Console.WriteLine("Invalid path");
            }

            return transactions;
        }
    }
}