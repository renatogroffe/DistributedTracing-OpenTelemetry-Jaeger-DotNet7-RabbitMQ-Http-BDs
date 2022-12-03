using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using ConsoleAppConsumoAPIs;
using ConsoleAppConsumoAPIs.Data;
using ConsoleAppConsumoAPIs.Documents;

Console.WriteLine("***** Tracing Distribuído com .NET 7 + ASP.NET Core + Jaeger + OpenTelemetry + " +
    "MongoDB + RabbitMQ + SQL Server + Redis + PostgreSQL *****");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(nameof(ConsoleAppConsumoAPIs))
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: nameof(ConsoleAppConsumoAPIs), serviceVersion: "1.0.0"))
    .AddHttpClientInstrumentation()
    .AddMongoDBInstrumentation()
    .AddJaegerExporter(exporter =>
    {
        exporter.AgentHost = "localhost";
        exporter.AgentPort = 6831;
    })
    .Build();

using var client = new HttpClient();
using var activitySource = new ActivitySource(nameof(ConsoleAppConsumoAPIs), "1.0.0");

while (true)
{
    using var activity = activitySource.StartActivity("SendRequests");
    activity?.SetTag("startPoint", "Program.cs");

    var contagemDocument = new ContagemDocument();
    contagemDocument.DataReferencia = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    contagemDocument.RedisData =
        Endpoints.SendRequest("Redis", Endpoints.APIContagemRedis, client);
    contagemDocument.RabbitMQData =
        Endpoints.SendRequest("RabbitMQ", Endpoints.APIContagemRabbitMQ, client);
    contagemDocument.PostgreSqlData =
        Endpoints.SendRequest("PostgreSQL", Endpoints.APIContagemPostgreSQL, client);

    ContagemRepository.Save(contagemDocument);

    activity?.Dispose();

    Console.WriteLine("Pressione ENTER para enviar uma nova requisição...");
    Console.ReadLine();
}