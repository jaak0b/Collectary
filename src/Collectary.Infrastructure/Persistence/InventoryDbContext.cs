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
    public DbSet<SharedField> SharedFields => Set<SharedField>();
    public DbSet<FieldGroup> FieldGroups => Set<FieldGroup>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CollectionShare> CollectionShares => Set<CollectionShare>();
    public DbSet<Tombstone> Tombstones => Set<Tombstone>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureSharedFields(modelBuilder);
        ConfigurePresets(modelBuilder);
        ConfigureItems(modelBuilder);
        ConfigureListEntries(modelBuilder);
        ConfigureFieldDefinitions(modelBuilder);
        ConfigureFieldValues(modelBuilder);
        ConfigureFieldGroups(modelBuilder);
        ConfigureAccounts(modelBuilder);
        ConfigureSyncState(modelBuilder);
        ConfigureClientGeneratedKeys(modelBuilder);
    }

    private static void ConfigureSyncState(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tombstone>(e =>
        {
            e.ToTable("Tombstones");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<SyncState>(e =>
        {
            e.ToTable("SyncState");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedNever();
        });
    }

    private static void ConfigureAccounts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasQueryFilter(u => !u.IsDeleted);
        });

        modelBuilder.Entity<CollectionShare>(e =>
        {
            e.ToTable("CollectionShares");
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.PresetId, s.SharedWithUserId }).IsUnique();
            e.HasQueryFilter(s => !s.IsDeleted);
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

    private static void ConfigureSharedFields(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SharedField>(e =>
        {
            e.ToTable("SharedFields");
            e.HasKey(sf => sf.Id);
            e.HasQueryFilter(sf => !sf.IsDeleted);
            e.HasOne(sf => sf.Definition)
             .WithOne()
             .HasForeignKey<FieldDefinition>(f => f.SharedFieldId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
        });

        modelBuilder.Entity<PresetSharedField>(e =>
        {
            e.ToTable("PresetSharedFields");
            e.HasKey(r => new { r.PresetId, r.SharedFieldId });
            e.HasOne(r => r.SharedField)
             .WithMany()
             .HasForeignKey(r => r.SharedFieldId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ListSharedField>(e =>
        {
            e.ToTable("ListSharedFields");
            e.HasKey(r => new { r.ListFieldDefinitionId, r.SharedFieldId });
            e.HasOne(r => r.SharedField)
             .WithMany()
             .HasForeignKey(r => r.SharedFieldId)
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
            e.HasMany(p => p.SharedFieldRefs)
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
            e.HasMany(d => d.SharedFieldRefs)
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
        modelBuilder.Entity<BarcodeFieldDefinition>().ToTable("BarcodeFieldDefinitions");
        modelBuilder.Entity<QrCodeFieldDefinition>().ToTable("QrCodeFieldDefinitions");
        modelBuilder.Entity<MultiImageFieldDefinition>().ToTable("MultiImageFieldDefinitions");
        modelBuilder.Entity<FileAttachmentFieldDefinition>().ToTable("FileAttachmentFieldDefinitions");
        modelBuilder.Entity<CountryFieldDefinition>().ToTable("CountryFieldDefinitions");
        modelBuilder.Entity<MeasurementFieldDefinition>().ToTable("MeasurementFieldDefinitions");
        modelBuilder.Entity<WeightFieldDefinition>().ToTable("WeightFieldDefinitions");
        modelBuilder.Entity<DateRangeFieldDefinition>().ToTable("DateRangeFieldDefinitions");
        modelBuilder.Entity<LinkedItemFieldDefinition>().ToTable("LinkedItemFieldDefinitions");
        modelBuilder.Entity<AudioFieldDefinition>().ToTable("AudioFieldDefinitions");
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
        modelBuilder.Entity<BarcodeFieldValue>(e =>
        {
            e.ToTable("BarcodeFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<QrCodeFieldValue>(e =>
        {
            e.ToTable("QrCodeFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<MultiImageFieldValue>(e =>
        {
            e.ToTable("MultiImageFieldValues");
            e.Ignore(v => v.Definition);
            e.Property(v => v.ImageKeys).HasConversion(
                v => string.Join('\n', v),
                v => v.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList());
        });
        modelBuilder.Entity<FileAttachmentFieldValue>(e =>
        {
            e.ToTable("FileAttachmentFieldValues");
            e.Ignore(v => v.Definition);
            e.OwnsMany(v => v.Files, b =>
            {
                b.ToTable("FileAttachmentEntries");
                b.WithOwner().HasForeignKey("OwnerValueId");
                b.Property(f => f.Key).IsRequired();
                b.Property(f => f.FileName);
                b.HasKey("OwnerValueId", "Key");
            });
        });
        modelBuilder.Entity<CountryFieldValue>(e =>
        {
            e.ToTable("CountryFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<MeasurementFieldValue>(e =>
        {
            e.ToTable("MeasurementFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<WeightFieldValue>(e =>
        {
            e.ToTable("WeightFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<DateRangeFieldValue>(e =>
        {
            e.ToTable("DateRangeFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<LinkedItemFieldValue>(e =>
        {
            e.ToTable("LinkedItemFieldValues");
            e.Ignore(v => v.Definition);
        });
        modelBuilder.Entity<AudioFieldValue>(e =>
        {
            e.ToTable("AudioFieldValues");
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
