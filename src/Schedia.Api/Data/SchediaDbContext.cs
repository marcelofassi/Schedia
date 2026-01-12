using Microsoft.EntityFrameworkCore;
using Schedia.Api.Data.Entities;
using HostEntity = Schedia.Api.Data.Entities.Host;

namespace Schedia.Api.Data;

public class SchediaDbContext : DbContext
{
    public SchediaDbContext(DbContextOptions<SchediaDbContext> options)
        : base(options)
    {
    }

    public DbSet<HostEntity> Hosts => Set<HostEntity>();
    public DbSet<LegalText> LegalTexts => Set<LegalText>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingIdempotency> BookingIdempotency => Set<BookingIdempotency>();
    public DbSet<BookingAudit> BookingAudits => Set<BookingAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HostEntity>(entity =>
        {
            entity.ToTable("Hosts");
            entity.HasKey(x => x.HostId);

            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RoleTitle).HasMaxLength(120);
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x => x.CalendarId).HasMaxLength(254).IsRequired();
            entity.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<LegalText>(entity =>
        {
            entity.ToTable("LegalTexts");
            entity.HasKey(x => x.LegalTextId);

            entity.Property(x => x.Lang).HasMaxLength(5).IsRequired();
            entity.Property(x => x.VersionLabel).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Body).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(false);
            entity.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasIndex(x => x.Lang)
                .IsUnique()
                .HasFilter("[IsActive] = 1");
        });

        modelBuilder.Entity<NotificationRecipient>(entity =>
        {
            entity.ToTable("NotificationRecipients");
            entity.HasKey(x => x.RecipientId);

            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings");
            entity.HasKey(x => x.BookingId);

            entity.Property(x => x.DurationMinutes).IsRequired();
            entity.Property(x => x.StartUtc).HasColumnType("datetime2(0)").IsRequired();
            entity.Property(x => x.EndUtc).HasColumnType("datetime2(0)").IsRequired();
            entity.Property(x => x.ClientName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ClientEmail).HasMaxLength(254).IsRequired();
            entity.Property(x => x.ClientCompany).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ClientPhone).HasMaxLength(40);
            entity.Property(x => x.ClientReason).HasMaxLength(400);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.GoogleEventId).HasMaxLength(256);
            entity.Property(x => x.GoogleMeetLink).HasMaxLength(1024);
            entity.Property(x => x.LegalAcceptedAtUtc).HasColumnType("datetime2(0)").IsRequired();
            entity.Property(x => x.LegalAcceptedIp).HasMaxLength(45);
            entity.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(x => x.Host)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.LegalText)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.LegalTextId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.HostId, x.StartUtc });
            entity.HasIndex(x => x.ClientEmail);

            entity.HasCheckConstraint("CK_Bookings_Duration", "[DurationMinutes] IN (30,45,60)");
            entity.HasCheckConstraint("CK_Bookings_Status", "[Status] IN ('booked','failed')");
        });

        modelBuilder.Entity<BookingIdempotency>(entity =>
        {
            entity.ToTable("BookingIdempotency");
            entity.HasKey(x => x.IdempotencyKey);

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2(0)");

            entity.HasOne(x => x.Booking)
                .WithMany(x => x.IdempotencyKeys)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BookingAudit>(entity =>
        {
            entity.ToTable("BookingAudit");
            entity.HasKey(x => x.BookingAuditId);

            entity.Property(x => x.EventType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.PayloadJson);
            entity.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(x => x.Booking)
                .WithMany(x => x.Audits)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.BookingId);
        });
    }
}
