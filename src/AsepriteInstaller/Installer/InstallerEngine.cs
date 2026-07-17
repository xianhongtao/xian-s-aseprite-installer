using AsepriteInstaller.Installer.Steps;
using AsepriteInstaller.State;
using AsepriteInstaller.Tui;
using Spectre.Console;

namespace AsepriteInstaller.Installer;

/// <summary>
/// Orchestrates the execution of all installer steps in order.
/// Handles idempotency (skip completed steps), error handling, and rollback.
/// </summary>
public sealed class InstallerEngine
{
    private readonly List<IInstallerStep> _steps;
    private readonly InstallContext _ctx;

    public InstallerEngine(InstallContext ctx, IEnumerable<IInstallerStep> steps)
    {
        _ctx = ctx;
        _steps = steps.ToList();
    }

    /// <summary>Run all steps sequentially. Returns true if all succeeded.</summary>
    public async Task<bool> RunAsync(CancellationToken ct = default)
    {
        var totalSteps = _steps.Count;
        var currentStep = 0;

        foreach (var step in _steps)
        {
            currentStep++;
            var prefix = $"[{currentStep}/{totalSteps}]";

            // --- Idempotency check ---
            if (step.CanSkip(_ctx))
            {
                _ctx.Log.Info($"{prefix} ⏭  Skipping '{step.DisplayName}' — already completed");
                _ctx.State.MarkSkipped(step.StepId, "Already completed");
                continue;
            }

            _ctx.Log.Info($"{prefix} ▶ Starting '{step.DisplayName}'");
            _ctx.State.MarkInProgress(step.StepId);

            try
            {
                var success = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("cyan"))
                    .StartAsync($"[cyan]{step.DisplayName}[/]",
                        _ => step.ExecuteAsync(_ctx, ct));

                if (success)
                {
                    _ctx.Log.Success($"{prefix} ✓ '{step.DisplayName}' completed");
                    _ctx.State.MarkCompleted(step.StepId);
                }
                else
                {
                    _ctx.Log.Error($"{prefix} ✗ '{step.DisplayName}' failed");
                    _ctx.State.MarkFailed(step.StepId, "Step returned false");

                    // Attempt cleanup.
                    try { await step.CleanupAsync(_ctx, ct); }
                    catch (Exception ex) { _ctx.Log.Warn($"Cleanup failed: {ex.Message}"); }

                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                _ctx.Log.Warn("Installation cancelled by user.");
                _ctx.State.MarkFailed(step.StepId, "Cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _ctx.Log.Error($"{prefix} ✗ '{step.DisplayName}' threw: {ex.Message}");
                _ctx.State.MarkFailed(step.StepId, $"{ex.GetType().Name}: {ex.Message}");

                // Attempt cleanup.
                try { await step.CleanupAsync(_ctx, ct); }
                catch (Exception cleanupEx) { _ctx.Log.Warn($"Cleanup failed: {cleanupEx.Message}"); }

                ConsoleApp.ShowError(ex.Message, _ctx.Log.LogFilePath);
                return false;
            }
        }

        return true;
    }
}
