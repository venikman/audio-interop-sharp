using Microsoft.EntityFrameworkCore;

namespace AudioSharp.App.Data;

public sealed class ConcernRepository : IConcernRepository
{
    private readonly ConcernsDbContext _dbContext;

    public ConcernRepository(ConcernsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ConcernRecord> AddAsync(ConcernRecord record, CancellationToken cancellationToken)
    {
        _dbContext.ConcernRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return record;
    }

    public async Task<IReadOnlyList<ConcernRecord>> GetRecentAsync(int count, CancellationToken cancellationToken)
    {
        return await _dbContext.ConcernRecords
            .AsNoTracking()
            .OrderByDescending(record => record.CreatedAtUtc.UtcDateTime)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
