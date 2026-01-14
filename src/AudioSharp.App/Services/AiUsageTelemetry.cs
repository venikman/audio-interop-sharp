using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public sealed class AiUsageTelemetry : IAiUsageTelemetry
{
    private const int MaxEvents = 8;
    private readonly object _sync = new();
    private readonly UsageCounterData _audio = new();
    private readonly UsageCounterData _text = new();
    private readonly List<AiTelemetryEvent> _events = [];

    public event Action? Changed;

    public AiUsageSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new AiUsageSnapshot(_audio.ToSnapshot(), _text.ToSnapshot());
        }
    }

    public IReadOnlyList<AiTelemetryEvent> GetRecentEvents()
    {
        lock (_sync)
        {
            return _events.ToList();
        }
    }

    public void RecordAudioTranscription(string provider, string model, bool success, TimeSpan duration)
    {
        Record(_audio, provider, model, success, duration, "Audio");
    }

    public void RecordTextCompletion(string provider, string model, bool success, TimeSpan duration)
    {
        Record(_text, provider, model, success, duration, "Text");
    }

    private void Record(
        UsageCounterData counter,
        string provider,
        string model,
        bool success,
        TimeSpan duration,
        string kind)
    {
        Action? handler;
        lock (_sync)
        {
            counter.Total++;
            if (success)
            {
                counter.Success++;
            }
            else
            {
                counter.Failure++;
            }

            counter.LastAtUtc = DateTimeOffset.UtcNow;
            counter.LastDuration = duration;
            counter.LastProvider = provider;
            counter.LastModel = model;
            _events.Insert(0, new AiTelemetryEvent(
                kind,
                provider,
                model,
                success,
                duration,
                counter.LastAtUtc.Value));

            if (_events.Count > MaxEvents)
            {
                _events.RemoveRange(MaxEvents, _events.Count - MaxEvents);
            }
            handler = Changed;
        }

        handler?.Invoke();
    }

    private sealed class UsageCounterData
    {
        public long Total { get; set; }
        public long Success { get; set; }
        public long Failure { get; set; }
        public TimeSpan? LastDuration { get; set; }
        public DateTimeOffset? LastAtUtc { get; set; }
        public string? LastProvider { get; set; }
        public string? LastModel { get; set; }

        public UsageCounter ToSnapshot()
        {
            return new UsageCounter(
                Total,
                Success,
                Failure,
                LastDuration,
                LastAtUtc,
                LastProvider,
                LastModel);
        }
    }
}
