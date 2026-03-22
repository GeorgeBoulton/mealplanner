using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace MealPlanner.Infrastructure.Data.Configurations;

public class ShoppingListItemConfiguration : IEntityTypeConfiguration<ShoppingListItem>
{
    public void Configure(EntityTypeBuilder<ShoppingListItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.IngredientName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Unit)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Map private backing field _fromRecipes to a jsonb column
        var fromRecipesComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property<List<string>>("_fromRecipes")
            .HasField("_fromRecipes")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("FromRecipes")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(fromRecipesComparer);
    }
}
