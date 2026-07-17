using AsepriteInstaller.State;

namespace AsepriteInstaller.Installer.Steps;

/// <summary>
/// One step in the installation pipeline.
/// Each step is idempotent (safe to re-run) and atomic (rolls back on failure).
/// </summary>
public interface IInstallerStep
{
    /// <summary>Unique identifier for this step (used in state.json).</summary>
    string StepId { get; }

    /// <summary>Human-readable name shown in the TUI.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Execute the step. Returns true on success, false on failure.
    /// Should be idempotent: if the step was already completed (per state),
    /// it should skip and return true.
    /// </summary>
    Task<bool> ExecuteAsync(InstallContext ctx, CancellationToken ct = default);

    /// <summary>
    /// Check whether this step can be skipped (already done).
    /// Called before ExecuteAsync to decide whether to run.
    /// </summary>
    bool CanSkip(InstallContext ctx) => ctx.State.IsCompleted(StepId) && !ctx.Options.Force;

    /// <summary>
    /// Clean up any partial work from a failed run.
    /// Called when ExecuteAsync returns false or throws.
    /// </summary>
    Task CleanupAsync(InstallContext ctx, CancellationToken ct = default) => Task.CompletedTask;
}
