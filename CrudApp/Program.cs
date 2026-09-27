using CrudApp.Data;
using CrudApp.Services;
using CrudApp.Telemetry;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddAppDatabase(builder.Configuration);
builder.Services.AddScoped<OrderSaga>();
builder.Services.AddScoped<OrderSimulator>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

// ---------------- OpenTelemetry ----------------
// Endpoint/protocol come from standard env vars:
//   OTEL_EXPORTER_OTLP_ENDPOINT (default http://localhost:4317), OTEL_EXPORTER_OTLP_PROTOCOL (grpc | http/protobuf)
//   OTEL_METRIC_EXPORT_INTERVAL (ms, default 60000), OTEL_RESOURCE_ATTRIBUTES
var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? AppTelemetry.ServiceName;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName, serviceVersion: AppTelemetry.ServiceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment.name"] = builder.Environment.EnvironmentName
        }))
    .WithTracing(tracing => tracing
        .AddSource(AppTelemetry.ActivitySourceName)
        .AddAspNetCoreInstrumentation(o =>
        {
            // Keep health probes and static files out of the traces.
            o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/healthz")
                              && !ctx.Request.Path.StartsWithSegments("/lib");
        })
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(AppTelemetry.MeterName)
        .AddMeter("System.Runtime")                 // built-in runtime metrics (.NET 9+)
        .AddAspNetCoreInstrumentation()             // http.server.request.duration, kestrel, ...
        .AddView("saga.duration", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = [0.05, 0.1, 0.25, 0.5, 0.75, 1, 1.5, 2, 3, 5]
        })
        .SetExemplarFilter(ExemplarFilterType.TraceBased) // link metric points to traces in Grafana
        .AddOtlpExporter())
    .WithLogging(
        logging => logging.AddOtlpExporter(),
        options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Demo only: creates the schema if missing. Use EF migrations for real projects.
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapHealthChecks("/healthz");

// Load generator for demos: POST /api/simulate?count=50
app.MapPost("/api/simulate", async (int? count, OrderSimulator simulator, CancellationToken ct) =>
    Results.Ok(await simulator.RunAsync(count ?? 20, ct)));

app.Run();
