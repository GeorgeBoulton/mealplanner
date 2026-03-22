using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Infrastructure.Data.Configurations;

public class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasMany(m => m.Entries)
            .WithOne()
            .HasForeignKey(e => e.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(m => m.Entries).HasField("_entries");
    }
}
