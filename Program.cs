using System.Text;
using System.Text.Json;
using concilliation_consumer;
using concilliation_consumer.Domain;
using concilliation_consumer.Dtos;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

ConnectionFactory factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "admin",
    Password = "admin"
};

if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
{
    Console.WriteLine("Please provide an id");
    Environment.Exit(1);
}

var id = args[0];

var connection = factory.CreateConnection();
var channel = connection.CreateModel();

channel.QueueDeclare(
    queue: "concilliation",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

Console.WriteLine($" [*] Waiting for messages on consumer {id}");

EventingBasicConsumer consumer = new(channel);

Console.WriteLine("Waiting for new messages");

consumer.Received += async (model, ea) =>
{

    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    if (message is null)
    {
        channel.BasicReject(ea.DeliveryTag, false);
        return;
    }

    try
    {
        Console.WriteLine(message);
        ConcilliationMessageDTO? dto = JsonSerializer.Deserialize<ConcilliationMessageDTO>(message);

        Console.WriteLine("Processing concilliation");
        Transactions transactions = await JSONReader.ReadFile(
            dto.Concilliation.FilePath,
            dto.Concilliation.Date,
            dto.PaymentProviderId
        );

        channel.BasicAck(ea.DeliveryTag, false);
    }
    catch
    {
        channel.BasicReject(ea.DeliveryTag, false);
    }


};

channel.BasicConsume(
    queue: "concilliation",
    autoAck: false,
    consumer: consumer
);

Console.ReadLine();

