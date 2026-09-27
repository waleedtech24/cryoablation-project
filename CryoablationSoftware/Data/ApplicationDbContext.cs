using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CryoablationSoftware.Models;
using System.Reflection.Emit;

namespace CryoablationSoftware.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Patient Related
        public DbSet<Patient> Patients { get; set; }
        public DbSet<EmergencyContact> EmergencyContacts { get; set; }
        public DbSet<ReferralInfo> ReferralInfos { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<ClinicalReport> ClinicalReports { get; set; }
        public DbSet<BoardApproval> BoardApprovals { get; set; }
        public DbSet<Procedure> Procedures { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<StockRecord> StockRecords { get; set; }
        public DbSet<UsageRecord> UsageRecords { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }

        // Procedure DbSets - ADD THESE
      //  public DbSet<Procedure> Procedures { get; set; }
        public DbSet<ProcedureItemAssignment> ProcedureItemAssignments { get; set; }
        public DbSet<ProcedureItemUsage> ProcedureItemUsages { get; set; }
        public DbSet<ProcedureClinicalReport> ProcedureClinicalReports { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }


        // phase 2
        public DbSet<Site> Sites { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // FAST search/sum ke liye helpful

            builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "Super Admin", NormalizedName = "SUPER ADMIN" },
            new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "Manager", NormalizedName = "MANAGER" }
            );


            builder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.LogType);
                entity.HasIndex(e => e.TableName);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsSuccessful);
                entity.HasIndex(e => e.RecordId);
            });
            builder.Entity<Item>(entity =>
            {
                entity.HasKey(e => e.ItemId);
                entity.Property(e => e.ItemCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ItemName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Unit).IsRequired().HasMaxLength(20);

                // Create unique index for ItemCode
                entity.HasIndex(e => e.ItemCode).IsUnique();
            });

            builder.Entity<StockRecord>(entity =>
            {
                entity.HasKey(e => e.StockRecordId);
                entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");

                // Foreign Key Relationships
                entity.HasOne(e => e.Item)
                      .WithMany(i => i.StockRecords)
                      .HasForeignKey(e => e.ItemId)
                      .OnDelete(DeleteBehavior.Restrict);

               
            });

            builder.Entity<UsageRecord>(entity =>
            {
                entity.HasKey(e => e.UsageRecordId);

                // Foreign Key Relationships
                entity.HasOne(e => e.Item)
                      .WithMany(i => i.UsageRecords)
                      .HasForeignKey(e => e.ItemId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Patient)
                      .WithMany()
                      .HasForeignKey(e => e.PatientId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Procedure)
                      .WithMany()
                      .HasForeignKey(e => e.ProcedureId)
                      .OnDelete(DeleteBehavior.SetNull);
            });



            // Procedure Configurations - ADD THESE
            builder.Entity<Procedure>(entity =>
            {
                entity.HasKey(e => e.ProcedureId);
                entity.Property(e => e.ProcedureROI).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ProcedureDuration).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProcedureEquipmentUsed).HasMaxLength(500);
                entity.Property(e => e.ProcedureStatus).IsRequired().HasMaxLength(50);

                // Foreign Key Relationships
                entity.HasOne(e => e.Patient)
                      .WithMany(p => p.Procedures)
                      .HasForeignKey(e => e.PatientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ProcedureItemAssignment>(entity =>
            {
                entity.HasKey(e => e.AssignmentId);
                entity.Property(e => e.AssignmentReason).IsRequired().HasMaxLength(200);

                // Foreign Key Relationships
                entity.HasOne(e => e.Procedure)
                      .WithMany(p => p.ItemAssignments)
                      .HasForeignKey(e => e.ProcedureId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Item)
                      .WithMany(i => i.ProcedureAssignments)
                      .HasForeignKey(e => e.ItemId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ProcedureItemUsage>(entity =>
            {
                entity.HasKey(e => e.UsageId);

                // Foreign Key Relationships
                entity.HasOne(e => e.Procedure)
                      .WithMany(p => p.ItemUsages)
                      .HasForeignKey(e => e.ProcedureId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Item)
                      .WithMany(i => i.ProcedureUsages)
                      .HasForeignKey(e => e.ItemId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ProcedureClinicalReport>(entity =>
            {
                entity.HasKey(e => e.ProcedureClinicalReportId);

                // Foreign Key Relationships
                entity.HasOne(e => e.Procedure)
                      .WithMany(p => p.ClinicalReports)
                      .HasForeignKey(e => e.ProcedureId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ClinicalReport)
                      .WithMany()
                      .HasForeignKey(e => e.ClinicalReportId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Unique constraint - same report can't be linked twice to same procedure
                entity.HasIndex(e => new { e.ProcedureId, e.ClinicalReportId }).IsUnique();
            });

            // Koi bhi custom config yahan likho agar zarurat ho
            // Example: builder.Entity<Patient>().Property(x => x.PatientName).IsRequired();
        }
    }
}
