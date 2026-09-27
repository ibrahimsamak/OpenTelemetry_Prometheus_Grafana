using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace CrudApp.Telemetry;

public static class AppTelemetry
{

    public const string ServiceName = "crud-app";
    public const string ServiceVersion = "1.0.0";
    public const string ActivitySourceName = "CrudApp";
    public const string MeterName = "CrudApp";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, ServiceVersion);
    public static readonly Meter Meter = new(MeterName, ServiceVersion);


    // Prometheus name: saga_outcomes_total{saga_name, saga_outcome, saga_failed_step}
    public static readonly Counter<long> SagaOutcomes = Meter.CreateCounter<long>(
        "saga.outcomes",
        description: "Number of finished sagas by outcome (completed | compensated | failed)");

    // Prometheus name: saga_duration_seconds_bucket / _sum / _count
    public static readonly Histogram<double> SagaDuration = Meter.CreateHistogram<double>(
        "saga.duration",
        unit: "s",
        description: "End-to-end saga duration"
    );

    // Prometheus name: orders_operations_total{ operation }
    public static readonly Counter<long> OrderOperations = Meter.CreateCounter<long>(
        "orders.operations",
        description: "CRUD operations executed on orders");
}