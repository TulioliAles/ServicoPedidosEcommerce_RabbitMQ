
using RabbitMQ.Client;
using ServicePedido.Model;
using System.Runtime.InteropServices;
using System.Text.Json;

const string exchangeName = "pedido.exchange";
const string queueName = "pedido.criados";
const string routingKey = "pedido.criado";

var factory = new RabbitMQ.Client.ConnectionFactory()
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest",
    VirtualHost = "/",
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
};

await using var connection = await factory.CreateConnectionAsync();
await using var channel  = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(
    exchange: exchangeName,
    type: RabbitMQ.Client.ExchangeType.Direct,
    durable: true,
    autoDelete: false,
    arguments: null);

await channel.QueueDeclareAsync(
    queue: queueName,
    durable: true,
    autoDelete: false,
    exclusive: false,
    arguments: null);

await channel.QueueBindAsync(
    queue: queueName,
    exchange: exchangeName,
    routingKey: routingKey,
    arguments: null);

await channel.BasicQosAsync(
    prefetchSize: 0,
    prefetchCount: 1,
    global: false);

var consumer = new RabbitMQ.Client.Events.AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (model, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var json = System.Text.Encoding.UTF8.GetString(body);
        var pedido = JsonSerializer.Deserialize<Pedido>(json);

        Console.WriteLine("========================");
        Console.WriteLine($"[Consumer] Pedido recebido: {pedido?.Id}");
        Console.WriteLine($"Cliente: {pedido?.ClienteEmail}");
        Console.WriteLine($"Valor: {pedido?.ValorTotal:C}");
        Console.WriteLine($"Criado: {pedido?.DataCriacao:O}");
        Console.WriteLine("========================");

        await Task.Delay(2000); // Simula o processamento do pedido

        await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
    }
    catch(JsonException ex)
    {
        Console.WriteLine($"[Consumer] Erro ao desserializar pedido: {ex.Message}");
        await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
        throw;
    }
    catch(Exception ex)
    {
        Console.WriteLine($"[Consumer] Erro ao processar pedido: {ex.Message}");
        await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
        throw;
    }
};

await channel.BasicConsumeAsync(
      queue: queueName,
      autoAck: false,
      consumer: consumer);

Console.WriteLine("Consumer iniciado. Pressione ENTER para sair.");
Console.ReadLine(); 




