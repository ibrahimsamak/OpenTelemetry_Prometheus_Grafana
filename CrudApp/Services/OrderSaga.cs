using System.Diagnostics;
using CrudApp.Data;
using CrudApp.Models;
using CrudApp.Telemetry;

namespace CrudApp.Services;

public enum SagaOutcome
{
    Completed,
    Compensated,
    Failed
}

public sealed class SagaStepException(string step, string message) : Exception(message)
{
    public string Step { get; } = step;
}

public class OrderSaga(AppDbContext db, IConfiguration config, ILogger<OrderSaga> logger)
{
    public const string SagaName = "order-fulfillment";
    private static readonly string[] Steps = ["reserve-inventory", "charge-payment", "ship-order"];
    private double StepFailureRate => config.GetValue("Saga:StepFailureRate", 0.15);
    private double CompensationFailureRate => config.GetValue("Saga:CompensationFailureRate", 0.10);

    public async Task<SagaOutcome> RunAsync(Order order, CancellationToken ct = default)
    {
        using var sagaActivity = AppTelemetry.ActivitySource.StartActivity($"saga {SagaName}");
        sagaActivity?.SetTag("saga.name", SagaName);
        sagaActivity?.SetTag("order.id", order.Id);

        var stopwatch = Stopwatch.StartNew();
        var completedSteps = new Stack<string>();
        string? failedStep = null;
        SagaOutcome outcome;
        logger.LogInformation("Saga {SagaName} started for order {OrderId}", SagaName, order.Id);

        try
        {
            foreach (var step in Steps)
            {
                await ExecuteStepAsync(step, ct);
                completedSteps.Push(step);
            }
            outcome = SagaOutcome.Completed;
        }
        catch (SagaStepException ex)
        {
            failedStep = ex.Step;
            logger.LogWarning(
                "Saga {SagaName} step {SagaStep} failed for order {OrderId}: {Reason}. Compensating {CompensationCount} step(s)",
                SagaName, ex.Step, order.Id, ex.Message, completedSteps.Count);

            outcome = await CompensateAsync(completedSteps, order, ct);
        }

        order.Status = outcome switch
        {
            SagaOutcome.Completed => OrderStatus.Completed,
            SagaOutcome.Compensated => OrderStatus.Compensated,
            _ => OrderStatus.Failed
        };

        await db.SaveChangesAsync(ct);
        stopwatch.Stop();
        var outcomeText = outcome.ToString().ToLowerInvariant();
        var tags = new TagList
        {
            { "saga.name", SagaName },
            { "saga.outcome", outcomeText },
            { "saga.failed_step", failedStep ?? "none" }
        };
        AppTelemetry.SagaOutcomes.Add(1, tags);
        AppTelemetry.SagaDuration.Record(stopwatch.Elapsed.TotalSeconds, tags);
        sagaActivity?.SetTag("saga.outcome", outcomeText);
        sagaActivity?.SetTag("saga.failed_step", failedStep ?? "none");
        if (outcome != SagaOutcome.Completed)
        {
            sagaActivity?.SetStatus(ActivityStatusCode.Error, $"saga {outcomeText}");
        }

        var level = outcome switch
        {
            SagaOutcome.Completed => LogLevel.Information,
            SagaOutcome.Compensated => LogLevel.Warning,
            _ => LogLevel.Error
        };
        logger.Log(level,
            "Saga {SagaName} for order {OrderId} finished with outcome {SagaOutcome} in {ElapsedMs} ms",
            SagaName, order.Id, outcomeText, stopwatch.ElapsedMilliseconds);

        return outcome;
    }


    private async Task ExecuteStepAsync(string step, CancellationToken ct)
    {
        using var activity = AppTelemetry.ActivitySource.StartActivity($"saga step {step}");
        activity?.SetTag("saga.step", step);

        // Simulates a call to a downstream service.
        await Task.Delay(Random.Shared.Next(20, 250), ct);
        if (Random.Shared.NextDouble() < StepFailureRate)
        {
            var ex = new SagaStepException(step, $"{step} rejected by downstream service");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw ex;
        }

    }
    private async Task<SagaOutcome> CompensateAsync(Stack<string> completedSteps, Order order, CancellationToken ct)
    {
        while (completedSteps.TryPop(out var step))
        {
            using var activity = AppTelemetry.ActivitySource.StartActivity($"saga compensate {step}");
            activity?.SetTag("saga.step", step);
            await Task.Delay(Random.Shared.Next(10, 120), ct);
            if (Random.Shared.NextDouble() < CompensationFailureRate)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "compensation failed");
                logger.LogError(
                    "Compensation of step {SagaStep} failed for order {OrderId}; manual intervention required",
                    step, order.Id);
                return SagaOutcome.Failed;
            }
        }
        return SagaOutcome.Compensated;
    }
}
