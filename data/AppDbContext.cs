using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using sara_coursework.models;
using sara_coursework.Services.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Awarded> Awarded { get; set; } = null!;
        public DbSet<Award> Awards { get; set; } = null!;
        public DbSet<AwardReason> AwardReasons { get; set; } = null!;
        public DbSet<AwardAssignment> AwardAssignments { get; set; } = null!;
        public DbSet<Decree> Decrees { get; set; } = null!;

        public DbSet<User> Users { get; set; }
        public DbSet<LogEntry> Logs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(SecretsManager.Secrets.DbConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Явно указываем имена таблиц
            modelBuilder.Entity<Award>().ToTable("Awards");
            modelBuilder.Entity<AwardReason>().ToTable("AwardReasons");
            modelBuilder.Entity<AwardAssignment>().ToTable("AwardAssignments");
            modelBuilder.Entity<Decree>().ToTable("Decrees");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<LogEntry>().ToTable("Logs");

            // Настройка наследования для Awarded (TPH)
            modelBuilder.Entity<Awarded>()
                .ToTable("Awarded")
                .HasDiscriminator<string>("AwardedType")
                .HasValue<Citizen>("Citizen")
                .HasValue<Collective>("Collective");

            // Уникальные индексы
            modelBuilder.Entity<Citizen>()
                .HasIndex(p => new { p.LastName, p.FirstName, p.Position})
                .IsUnique();

            modelBuilder.Entity<Collective>()
                .HasIndex(c => c.CollectiveName)
                .IsUnique();

            modelBuilder.Entity<Award>()
                .HasIndex(a => a.AwardName)
                .IsUnique();

            modelBuilder.Entity<AwardReason>()
                .HasIndex(ar => ar.ReasonName)
                .IsUnique();

            modelBuilder.Entity<Decree>()
                .HasIndex(d => new { d.Number, d.Date })
                .IsUnique();

            // Настройка связей для AwardAssignment
            modelBuilder.Entity<AwardAssignment>()
                .HasOne(aa => aa.Awarded)
                .WithMany(a => a.AwardAssignments)
                .HasForeignKey(aa => aa.AwardedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AwardAssignment>()
                .HasOne(aa => aa.Award)
                .WithMany()
                .HasForeignKey(aa => aa.AwardId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AwardAssignment>()
                .HasOne(aa => aa.Decree)
                .WithMany(d => d.AwardAssignments)
                .HasForeignKey(aa => aa.DecreeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связи Decree -> AwardReason
            modelBuilder.Entity<Decree>()
                .HasOne(d => d.AwardReason)
                .WithMany()
                .HasForeignKey(d => d.AwardReasonId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка связи гражданин-коллектив (один-ко-многим)
            modelBuilder.Entity<Citizen>()
                .HasOne(c => c.Collective)
                .WithMany()
                .HasForeignKey(c => c.CollectiveId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка связи коллектив -> граждане
            modelBuilder.Entity<Collective>()
                .HasMany(c => c.Members)
                .WithOne(c => c.Collective)
                .HasForeignKey(c => c.CollectiveId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Citizen>(entity =>
            {
                entity.Property(e => e.LastName)
                    .HasConversion(
                        v => AesHelper.Encrypt(v),
                        v => AesHelper.Decrypt(v));

                entity.Property(e => e.FirstName)
                    .HasConversion(
                        v => AesHelper.Encrypt(v),
                        v => AesHelper.Decrypt(v));

                entity.Property(e => e.MiddleName)
                    .HasConversion(
                        v => v != null ? AesHelper.Encrypt(v) : null,
                        v => v != null ? AesHelper.Decrypt(v) : null);
            });
        }

        public static void InitializeDatabase(AppDbContext context)
        {
            if (!context.Users.Any())
            {
                var (hash, salt) = PasswordHasher.CreateHash("admin123");

                context.Users.Add(new User
                {
                    Username = "admin",
                    PasswordHash = hash,
                    Salt = salt,
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow
                });

                context.SaveChanges();
            }
        }
    }
}
