using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Logging;
using Collectary.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class SystemFieldRepository : ISystemFieldRepository
{
    private readonly Func<InventoryDbContext> _dbFactory;
    private readonly IFieldDefinitionMerger _merger;
    private readonly IAppLogger _logger;

    public SystemFieldRepository(Func<InventoryDbContext> dbFactory, IFieldDefinitionMerger merger, IAppLogger? logger = null)
    {
        _dbFactory = dbFactory;
        _merger = merger;
        _logger = logger ?? new NullAppLogger();
    }

    public async Task<IReadOnlyList<SystemField>> GetAllAsync()
    {
        using var db = _dbFactory();
        return await db.SystemFields
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).SubFields)
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).Groups)
            .OrderBy(sf => sf.SortOrder)
            .ThenBy(sf => sf.Name)
            .ToListAsync();
    }

    public async Task<SystemField?> GetByIdAsync(Guid id)
    {
        using var db = _dbFactory();
        return await db.SystemFields
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).SubFields)
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).Groups)
            .FirstOrDefaultAsync(sf => sf.Id == id);
    }

    public async Task AddAsync(SystemField field)
    {
        using var db = _dbFactory();
        field.SortOrder = await db.SystemFields.AnyAsync()
            ? await db.SystemFields.MaxAsync(sf => sf.SortOrder) + 1
            : 0;
        db.SystemFields.Add(field);
        await db.SaveChangesAsync();
    }

    public async Task ReorderAsync(IReadOnlyList<Guid> orderedIds)
    {
        using var db = _dbFactory();
        var fields = await db.SystemFields.ToListAsync();
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var field = fields.FirstOrDefault(sf => sf.Id == orderedIds[i]);
            if (field is not null)
                field.SortOrder = i;
        }
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SystemField field)
    {
        using var db = _dbFactory();
        var tracked = await db.SystemFields
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).SubFields)
            .Include(sf => sf.Definition)
            .ThenInclude(d => ((ListFieldDefinition)d).Groups)
            .FirstOrDefaultAsync(sf => sf.Id == field.Id);
        if (tracked is null) return;

        tracked.Name = field.Name;
        _merger.Apply(db, tracked.Definition, field.Definition);

        await db.SaveChangesAsync();
        _logger.Debug("Updated system field id={Id} name={Name}", field.Id, field.Name);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var db = _dbFactory();
        var field = await db.SystemFields.FindAsync(id);
        if (field is not null)
        {
            db.SystemFields.Remove(field);
            await db.SaveChangesAsync();
        }
    }
}
