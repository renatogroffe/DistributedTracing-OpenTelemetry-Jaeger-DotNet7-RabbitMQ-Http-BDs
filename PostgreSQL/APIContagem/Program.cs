using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Npgsql;
using APIContagem.Data;
using APIContagem.Tracing;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

var builder = WebApplication.CreateBuilder(args);

// Documentacao do OpenTelemetry:
// https://opentelemetry.io/docs/instrumentation/net/getting-started/

// Integracao do OpenTelemetry com Jaeger:
// https://opentelemetry.io/docs/instrumentation/net/exporters/

// Documentacaoo do Jaeger:
// https://www.jaegertracing.io/docs/1.39/

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ContagemRepository>();

builder.Services.AddDbContext<ContagemContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("BaseContagem"));
});

builder.Services.AddOpenTelemetryTracing(traceProvider =>
{
    traceProvider
        .AddSource(OpenTelemetryExtensions.ServiceName)
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
                    serviceVersion: OpenTelemetryExtensions.ServiceVersion))
        .AddAspNetCoreInstrumentation()
        .AddNpgsql()
        .AddJaegerExporter(exporter =>
        {
            exporter.AgentHost = builder.Configuration["Jaeger:AgentHost"];
            exporter.AgentPort = Convert.ToInt32(builder.Configuration["Jaeger:AgentPort"]);
        });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();