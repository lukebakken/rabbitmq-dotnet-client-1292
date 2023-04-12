using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var cf = new ConnectionFactory()
{
    HostName = "localhost",
    VirtualHost = "/",
    Port = 5672,
    UserName = "guest",
    Password = "guest",
};

var connectionShutdownEvent = new ManualResetEventSlim();
var modelShutdownEvent = new ManualResetEventSlim();
var consumerCancelledEvent = new ManualResetEventSlim();

var connection = cf.CreateConnection("Test");

connection.ConnectionShutdown += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}] connection.shutdown");
    connectionShutdownEvent.Set();
};

var model = connection.CreateModel();
var queueDeclareResult = model.QueueDeclare(exclusive: true);
string queueName = queueDeclareResult.QueueName;

model.ModelShutdown += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}] model.shutdown");
    modelShutdownEvent.Set();
};

var consumer = new EventingBasicConsumer(model);

string? consumerTag = null;

consumer.Received += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}] received");

    model.BasicAck(e.DeliveryTag, false);
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}] acked");

    model.BasicCancel(consumerTag!);
    consumerCancelledEvent.Set();
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}] consumer cancelled");
};

consumerTag = model.BasicConsume(queueName, false, consumer);

Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}] consuming has started, waiting for consumer cancellation...");
consumerCancelledEvent.Wait();

Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}] saw consumer cancellation, closing and disposing model...");
model.Close();
model.Dispose();
modelShutdownEvent.Wait();

Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}] model disposed, closing and disposing connection...");
connection.Close();
connection.Dispose();
connectionShutdownEvent.Wait();

Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}] done executing");
