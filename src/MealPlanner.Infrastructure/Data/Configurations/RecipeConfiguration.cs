using MealPlanner.Domain.Entities;
using MealPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace MealPlanner.Infrastructure.Data.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(2000);

        builder.Property(r => r.Instructions)
            .IsRequired();

        builder.Property(r => r.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.OwnsMany(r => r.Ingredients, ib =>
        {
            ib.ToTable("RecipeIngredients");
            ib.WithOwner().HasForeignKey("RecipeId");
            ib.HasKey("RecipeId", "Name");
            ib.Property(i => i.Name).HasMaxLength(200).IsRequired();
            ib.Property(i => i.Unit).HasMaxLength(50).IsRequired();
            ib.Property(i => i.ShoppingCategory)
                .HasConversion<string>()
                .HasMaxLength(50);
        });
        builder.Navigation(r => r.Ingredients).HasField("_ingredients");

        // Map private backing field _tags to a jsonb column
        var tagsComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property<List<string>>("_tags")
            .HasField("_tags")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Tags")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(tagsComparer);
    }
}
