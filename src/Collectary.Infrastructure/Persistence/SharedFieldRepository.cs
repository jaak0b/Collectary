using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Logging;
using Collectary.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class SharedFieldRepository : ISharedFieldRepository
{
    private readonly Func<InventoryDbContext> _dbFactory;
    private readonly IFieldDefinitionMerger _merger;
    private readonly IAppLogger _logger;
    private readonly ICurrentUser? _currentUser;

    public SharedFieldRepository(Func<InventoryDbContext> dbFactory, IFieldDefinitionMerger merger, IAppLogger? logger = null, ICurrentUser? currentUser = null)
    {
        _dbFactory = dbFactory;
        _merger = merger;
        _logger = logger ?? new NullAppLogger();
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SharedField>> GetAllAsync()
    {
        using var db = _dbFactory();
        return await db.SharedFields
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).SubFields)
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).Groups)
            .OrderBy(sf => sf.SortOrder)
            .ThenBy(sf => sf.Name)
            .ToListAsync();
    }

    public async Task<SharedField?> GetByIdAsync(Guid id)
    {
        using var db = _dbFactory();
        return await db.SharedFields
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).SubFields)
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).Groups)
            .FirstOrDefaultAsync(sf => sf.Id == id);
    }

    public async Task AddAsync(SharedField field)
    {
        using var db = _dbFactory();
        field.SortOrder = await db.SharedFields.AnyAsync()
            ? await db.SharedFields.MaxAsync(sf => sf.SortOrder) + 1
            : 0;
        field.UpdatedAt = DateTime.UtcNow;
        ((ISyncable)field).StampModified(_currentUser?.AuthenticatedId);
        db.SharedFields.Add(field);
        await db.SaveChangesAsync();
    }

    public async Task ReorderAsync(IReadOnlyList<Guid> orderedIds)
    {
        using var db = _dbFactory();
        var fields = await db.SharedFields.ToListAsync();
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var field = fields.FirstOrDefault(sf => sf.Id == orderedIds[i]);
            if (field is not null)
                field.SortOrder = i;
        }
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SharedField field)
    {
        using var db = _dbFactory();
        var tracked = await db.SharedFields
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).SubFields)
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).Groups)
            .FirstOrDefaultAsync(sf => sf.Id == field.Id);
        if (tracked is null) return;

        tracked.Name = field.Name;
        _merger.Apply(db, tracked.Definition, field.Definition);
        tracked.UpdatedAt = DateTime.UtcNow;
        ((ISyncable)tracked).StampModified(_currentUser?.AuthenticatedId);

        await db.SaveChangesAsync();
        _logger.Debug("Updated shared field id={Id} name={Name}", field.Id, field.Name);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var db = _dbFactory();
        var field = await db.SharedFields.FindAsync(id);
        if (field is null) return;

        db.SharedFields.Remove(field);
        db.Tombstones.Add(new Tombstone { Id = id });

        await db.SaveChangesAsync();
    }
}
