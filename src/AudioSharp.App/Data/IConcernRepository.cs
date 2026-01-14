namespace AudioSharp.App.Data;

public interface IConcernRepository
{
    Task<ConcernRecord> AddAsync(ConcernRecord record, CancellationToken cancellationToken);
    Task<IReadOnlyList<ConcernRecord>> GetRecentAsync(int count, CancellationToken cancellationToken);
}
