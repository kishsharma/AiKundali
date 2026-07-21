using System.Collections.Concurrent;

namespace KundaliEngine.API.Services.CurrentLifeFocus;

public interface ICurrentLifeFocusAnalysisStore
{
    void Save(CurrentLifeFocusSnapshot snapshot, TimeSpan expiry);

    bool TryGet(Guid analysisId, Guid? userId, out CurrentLifeFocusSnapshot snapshot);

    void RecordSelection(Guid analysisId, string selectedAreaKey, string? specificConcernKey);
}

public class CurrentLifeFocusAnalysisStore : ICurrentLifeFocusAnalysisStore
{
    private readonly ConcurrentDictionary<Guid, StoredSnapshot> _snapshots = new();

    public void Save(CurrentLifeFocusSnapshot snapshot, TimeSpan expiry)
    {
        _snapshots[snapshot.AnalysisId] = new StoredSnapshot(snapshot, DateTime.UtcNow.Add(expiry));
    }

    public bool TryGet(Guid analysisId, Guid? userId, out CurrentLifeFocusSnapshot snapshot)
    {
        snapshot = new CurrentLifeFocusSnapshot();
        if (!_snapshots.TryGetValue(analysisId, out var stored))
        {
            return false;
        }

        if (stored.ExpiresAtUtc <= DateTime.UtcNow)
        {
            _snapshots.TryRemove(analysisId, out _);
            return false;
        }

        if (stored.Snapshot.UserId != userId)
        {
            return false;
        }

        snapshot = stored.Snapshot;
        return true;
    }

    public void RecordSelection(Guid analysisId, string selectedAreaKey, string? specificConcernKey)
    {
        if (_snapshots.TryGetValue(analysisId, out var stored))
        {
            stored.SelectedAreaKey = selectedAreaKey;
            stored.SpecificConcernKey = specificConcernKey;
        }
    }

    private sealed class StoredSnapshot
    {
        public StoredSnapshot(CurrentLifeFocusSnapshot snapshot, DateTime expiresAtUtc)
        {
            Snapshot = snapshot;
            ExpiresAtUtc = expiresAtUtc;
        }

        public CurrentLifeFocusSnapshot Snapshot { get; }

        public DateTime ExpiresAtUtc { get; }

        public string? SelectedAreaKey { get; set; }

        public string? SpecificConcernKey { get; set; }
    }
}
