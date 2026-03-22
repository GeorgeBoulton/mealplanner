using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Infrastructure.Data.Configurations;

public class FridgeItemConfiguration : IEntityTypeConfiguration<FridgeItem>
{
    public void Configure(EntityTypeBuilder<FridgeItem> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Unit)
            .HasMaxLength(50);
    }
}
