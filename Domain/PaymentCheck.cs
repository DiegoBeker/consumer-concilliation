namespace concilliation_consumer;
public class PaymentCheck(long id, string status)
{
    public long Id { get; set;} = id;
    public string Status { get; set;} = status;
}