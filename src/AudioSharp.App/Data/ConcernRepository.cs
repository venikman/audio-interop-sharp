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
        var records = await _dbContext.ConcernRecords
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .OrderByDescending(record => record.CreatedAtUtc)
            .Take(count)
            .ToList();
    }
}
