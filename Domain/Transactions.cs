namespace concilliation_consumer.Domain;

public class Transactions
{
    public List<PaymentCheck> DataBaseToFile { get; } = new List<PaymentCheck>();
    public List<PaymentCheck> FileToDatabase { get; } = new List<PaymentCheck>();
    public List<PaymentCheck> DifferentStatus { get; } = new List<PaymentCheck>();
}