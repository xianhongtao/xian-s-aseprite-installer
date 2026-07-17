using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsepriteInstaller.State;

/// <summary>
/// Persistent record of one step's completion, used for idempotency.
/// </summary>
public sealed class StepRecord
{
    public string StepId { get; set; } = string.Empty;
    public StepStatus Status { get; set; } = StepStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public string? Checksum { get; set; }
    public string? Version { get; set; }
    public string? Message { get; set; }
}

public enum StepStatus { Pending, InProgress, Completed, Failed, Skipped }

/// <summary>
/// The full on-disk state file (state.json) that tracks every step.
/// Re-running the installer reads this to skip already-completed steps.
/// </summary>
public sealed class InstallState
{
    public string SchemaVersion { get; set; } = "1";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<StepRecord> Steps { get; set; } = [];

    [JsonIgnore]
    public string FilePath { get; set; } = string.Empty;

    // ------------------------------------------------------------------
    //  Load / Save
    // ------------------------------------------------------------------

    public static InstallState LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize(json, InstallStateJsonContext.Default.InstallState) ?? new InstallState();
                state.FilePath = path;
                if (state.CreatedAt == null)
                    state.CreatedAt = DateTime.Now;
                return state;
            }
            catch
            {
                // Corrupt state — start fresh.
            }
        }

        return new InstallState
        {
            FilePath = path,
            CreatedAt = DateTime.Now,
            Steps = [],
        };
    }

    public void Save()
    {
        UpdatedAt = DateTime.Now;
        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // Atomic write: temp file → rename.
        var tmp = FilePath + ".tmp";
        var json = JsonSerializer.Serialize(this, InstallStateJsonContext.Default.InstallState);
        File.WriteAllText(tmp, json);

        if (File.Exists(FilePath))
            File.Delete(FilePath);
        File.Move(tmp, FilePath);
    }

    // ------------------------------------------------------------------
    //  Step helpers
    // ------------------------------------------------------------------

    public StepRecord? GetStep(string stepId) =>
        Steps.FirstOrDefault(s => s.StepId == stepId);

    public bool IsCompleted(string stepId)
    {
        var s = GetStep(stepId);
        return s is { Status: StepStatus.Completed };
    }

    public void MarkInProgress(string stepId)
    {
        var s = GetStep(stepId) ?? new StepRecord { StepId = stepId };
        s.Status = StepStatus.InProgress;
        Upsert(s);
        Save();
    }

    public void MarkCompleted(string stepId, string? checksum = null, string? version = null, string? message = null)
    {
        var s = GetStep(stepId) ?? new StepRecord { StepId = stepId };
        s.Status = StepStatus.Completed;
        s.CompletedAt = DateTime.Now;
        s.Checksum = checksum;
        s.Version = version;
        s.Message = message;
        Upsert(s);
        Save();
    }

    public void MarkFailed(string stepId, string message)
    {
        var s = GetStep(stepId) ?? new StepRecord { StepId = stepId };
        s.Status = StepStatus.Failed;
        s.Message = message;
        Upsert(s);
        Save();
    }

    public void MarkSkipped(string stepId, string? message = null)
    {
        var s = GetStep(stepId) ?? new StepRecord { StepId = stepId };
        s.Status = StepStatus.Skipped;
        s.CompletedAt = DateTime.Now;
        s.Message = message;
        Upsert(s);
        Save();
    }

    public void ResetStep(string stepId)
    {
        var s = GetStep(stepId);
        if (s != null)
        {
            Steps.Remove(s);
            Save();
        }
    }

    private void Upsert(StepRecord record)
    {
        var idx = Steps.FindIndex(s => s.StepId == record.StepId);
        if (idx >= 0)
            Steps[idx] = record;
        else
            Steps.Add(record);
    }
}
