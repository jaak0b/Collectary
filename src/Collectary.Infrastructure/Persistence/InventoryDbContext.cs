using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Collectary.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public DbSet<Preset> Presets => Set<Preset>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<FieldValue> FieldValues => Set<FieldValue>();
    public DbSet<ListEntry> ListEntries => Set<ListEntry>();
    public DbSet<SystemField> SystemFields => Set<SystemField>();
    public DbSet<FieldGroup> FieldGroups => Set<FieldGroup>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserCredentialRecord> UserCredentials => Set<UserCredentialRecord>();
    public DbSet<CollectionShare> CollectionShares => Set<CollectionShare>();

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureSystemFields(modelBuilder);
        ConfigurePresets(modelBuilder);
        ConfigureItems(modelBuilder);
        ConfigureListEntries(modelBuilder);
        ConfigureFieldDefinitions(modelBuilder);
        ConfigureFieldValues(modelBuilder);
        ConfigureFieldGroups(modelBuilder);
        ConfigureAccounts(modelBuilder);
        ConfigureClientGeneratedKeys(modelBuilder);
    }

    private static void ConfigureAccounts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<UserCredentialRecord>(e =>
        {
            e.ToTable("UserCredentials");
            e.HasKey(c => c.UserId);
        });

        modelBuilder.Entity<CollectionShare>(e =>
        {
            e.ToTable("CollectionShares");
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.PresetId, s.SharedWithUserId }).IsUnique();
        });
    }

    private static void ConfigureFieldGroups(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldGroup>(e =>
        {
            e.ToTable("FieldGroups");
            e.HasKey(g => g.Id);
            e.HasOne<FieldGroup>()
             .WithMany()
             .HasForeignKey(g => g.ParentGroupId)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);
        });
    }

    private static void ConfigureClientGeneratedKeys(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var idProperty = entityType.FindProperty(nameof(DomainObject.Id));
            if (idProperty is not null)
                idProperty.ValueGenerated = ValueGenerated.Never;
        }
    }

    private static void ConfigureSystemFields(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemField>(e =>
        {
            e.ToTable("SystemFields");
            e.HasKey(sf => sf.Id);
            e.HasQueryFilter(sf => !sf.IsDeleted);
            e.HasOne(sf => sf.Definition)
             .WithOne()
             .HasForeignKey<FieldDefinition>(f => f.SystemFieldId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
        });

        modelBuilder.Entity<PresetSystemField>(e =>
        {
            e.ToTable("PresetSystemFields");
            e.HasKey(r => new { r.PresetId, r.SystemFieldId });
            e.HasOne(r => r.SystemField)
             .WithMany()
             .HasForeignKey(r => r.SystemFieldId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ListSystemField>(e =>
        {
            e.ToTable("ListSystemFields");
            e.HasKey(r => new { r.ListFieldDefinitionId, r.SystemFieldId });
            e.HasOne(r => r.SystemField)
             .WithMany()
             .HasForeignKey(r => r.SystemFieldId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePresets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Preset>(e =>
        {
            e.ToTable("Presets");
            e.HasKey(p => p.Id);
            e.HasQueryFilter(p => !p.IsDeleted);
            e.HasMany(p => p.Fields)
             .WithOne()
             .HasForeignKey(f => f.PresetId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
            e.HasMany(p => p.SystemFieldRefs)
             .WithOne()
             .HasForeignKey(r => r.PresetId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Groups)
             .WithOne()
             .HasForeignKey(g => g.PresetId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
            e.HasOne<Preset>()
             .WithMany()
             .HasForeignKey(p => p.ParentPresetId)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);
        });
    }

    private static void ConfigureItems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(e =>
        {
            e.ToTable("Items");
            e.HasKey(i => i.Id);
            e.HasQueryFilter(i => !i.IsDeleted);
            e.HasMany(i => i.Values)
             .WithOne()
             .HasForeignKey(v => v.ItemId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
        });
    }

    private static void ConfigureListEntries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ListEntry>(e =>
        {
            e.ToTable("ListEntries");
            e.HasKey(le => le.Id);
            e.HasMany(le => le.SubValues)
             .WithOne()
             .HasForeignKey(v => v.ListEntryId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
        });
    }

    private static void ConfigureFieldDefinitions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldDefinition>(e =>
        {
            e.ToTable("FieldDefinitions");
            e.HasKey(f => f.Id);
            e.Ignore(f => f.ValueType);
        });

        modelBuilder.Entity<ListFieldDefinition>(e =>
        {
            e.ToTable("ListFieldDefinitions");
            e.HasMany(d => d.SubFields)
             .WithOne()
             .HasForeignKey(f => f.ParentListFieldDefinitionId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
            e.HasMany(d => d.SystemFieldRefs)
             .WithOne()
             .HasForeignKey(r => r.ListFieldDefinitionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(d => d.Groups)
             .WithOne()
             .HasForeignKey(g => g.ParentListFieldDefinitionId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
        });
        modelBuilder.Entity<DisplayNameFieldDefinition>().ToTable("DisplayNameFieldDefinitions");
        modelBuilder.Entity<TextFieldDefinition>().ToTable("TextFieldDefinitions");
        modelBuilder.Entity<IntegerFieldDefinition>().ToTable("IntegerFieldDefinitions");
        modelBuilder.Entity<DecimalFieldDefinition>().ToTable("DecimalFieldDefinitions");
        modelBuilder.Entity<ColorFieldDefinition>().ToTable("ColorFieldDefinitions");
        modelBuilder.Entity<ImageFieldDefinition>().ToTable("ImageFieldDefinitions");
        modelBuilder.Entity<DateFieldDefinition>().ToTable("DateFieldDefinitions");
        modelBuilder.Entity<RatingFieldDefinition>().ToTable("RatingFieldDefinitions");
        modelBuilder.Entity<BoolFieldDefinition>().ToTable("BoolFieldDefinitions");
        modelBuilder.Entity<UrlFieldDefinition>().ToTable("UrlFieldDefinitions");
        modelBuilder.Entity<RichTextFieldDefinition>().ToTable("RichTextFieldDefinitions");
        modelBuilder.Entity<PhoneFieldDefinition>().ToTable("PhoneFieldDefinitions");
        modelBuilder.Entity<EmailFieldDefinition>().ToTable("EmailFieldDefinitions");
        modelBuilder.Entity<PercentageFieldDefinition>().ToTable("PercentageFieldDefinitions");
        modelBuilder.Entity<DurationFieldDefinition>().ToTable("DurationFieldDefinitions");
        modelBuilder.Entity<TimeFieldDefinition>().ToTable("TimeFieldDefinitions");
        modelBuilder.Entity<CurrencyFieldDefinition>().ToTable("CurrencyFieldDefinitions");
        modelBuilder.Entity<TagsFieldDefinition>().ToTable("TagsFieldDefinitions");
        modelBuilder.Entity<SingleChoiceFieldDefinition>(e =>
        {
            e.ToTable("SingleChoiceFieldDefinitions");
            e.OwnsMany(d => d.Choices, b =>
            {
                b.ToTable("SingleChoiceOptions");
                b.Property(c => c.Id).ValueGeneratedNever();
                b.HasKey(c => c.Id);
                b.Property(c => c.Value).IsRequired();
            });
        });
        modelBuilder.Entity<MultiChoiceFieldDefinition>(e =>
        {
            e.ToTable("MultiChoiceFieldDefinitions");
            e.OwnsMany(d => d.Choices, b =>
            {
                b.ToTable("MultiChoiceOptions");
                b.Property(c => c.Id).ValueGeneratedNever();
                b.HasKey(c => c.Id);
                b.Property(c => c.Value).IsRequired();
            });
        });
    }

    private static void ConfigureFieldValues(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldValue>(e =>
        {
            e.ToTable("FieldValues");
            e.HasKey(v => v.Id);
            e.HasOne<FieldDefinition>()
             .WithMany()
             .HasForeignKey(v => v.FieldDefinitionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ListFieldValue>(e =>
        {
            e.ToTable("ListFieldValues");
            e.HasMany(v => v.Entries)
             .WithOne()
             .HasForeignKey(le => le.ListFieldValueId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<TextFieldValue>(e =>
        {
            e.ToTable("TextFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<IntegerFieldValue>(e =>
        {
            e.ToTable("IntegerFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<DecimalFieldValue>(e =>
        {
            e.ToTable("DecimalFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<ColorFieldValue>(e =>
        {
            e.ToTable("ColorFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<ImageFieldValue>(e =>
        {
            e.ToTable("ImageFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<DateFieldValue>(e =>
        {
            e.ToTable("DateFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<RatingFieldValue>(e =>
        {
            e.ToTable("RatingFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<BoolFieldValue>(e =>
        {
            e.ToTable("BoolFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<UrlFieldValue>(e =>
        {
            e.ToTable("UrlFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<RichTextFieldValue>(e =>
        {
            e.ToTable("RichTextFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<PhoneFieldValue>(e =>
        {
            e.ToTable("PhoneFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<EmailFieldValue>(e =>
        {
            e.ToTable("EmailFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<PercentageFieldValue>(e =>
        {
            e.ToTable("PercentageFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<DurationFieldValue>(e =>
        {
            e.ToTable("DurationFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<TimeFieldValue>(e =>
        {
            e.ToTable("TimeFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<CurrencyFieldValue>(e =>
        {
            e.ToTable("CurrencyFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<TagsFieldValue>(e =>
        {
            e.ToTable("TagsFieldValues");
            e.Ignore(v => v.Definition);
            e.Property(v => v.Tags).HasConversion(
                v => string.Join('\n', v),
                v => v.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList());
        });
        modelBuilder.Entity<SingleChoiceFieldValue>(e =>
        {
            e.ToTable("SingleChoiceFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<MultiChoiceFieldValue>(e =>
        {
            e.ToTable("MultiChoiceFieldValues");
            e.Ignore(v => v.Definition);
            e.Property(v => v.Selected).HasConversion(
                v => string.Join('\n', v),
                v => v.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList());
        });
    }
}
