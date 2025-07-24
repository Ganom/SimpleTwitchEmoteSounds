using Microsoft.EntityFrameworkCore;
using SimpleTwitchEmoteSounds.Data.Entities;

namespace SimpleTwitchEmoteSounds.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppSettingsEntity> AppSettings { get; set; }
    public DbSet<UserStateEntity> UserStates { get; set; }
    public DbSet<SoundCommandEntity> SoundCommands { get; set; }
    public DbSet<SoundFileEntity> SoundFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettingsEntity>(entity =>
        {
            entity.HasMany(e => e.SoundCommands)
                .WithOne(e => e.AppSettings)
                .HasForeignKey(e => e.AppSettingsId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SoundCommandEntity>(entity =>
        {
            entity.HasMany(e => e.SoundFiles)
                .WithOne(e => e.SoundCommand)
                .HasForeignKey(e => e.SoundCommandId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}