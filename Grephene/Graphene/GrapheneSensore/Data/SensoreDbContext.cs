using GrapheneSensore.Models;
using GrapheneSensore.Configuration;
using GrapheneSensore.Logging;
using Microsoft.EntityFrameworkCore;
using System;

namespace GrapheneSensore.Data
{
    public class SensoreDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<PressureMapData> PressureMapData => Set<PressureMapData>();
        public DbSet<Alert> Alerts => Set<Alert>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<MetricsSummary> MetricsSummaries => Set<MetricsSummary>();
        public DbSet<Template> Templates => Set<Template>();
        public DbSet<Section> Sections => Set<Section>();
        public DbSet<TemplateSectionLink> TemplateSectionLinks => Set<TemplateSectionLink>();
        public DbSet<Code> Codes => Set<Code>();
        public DbSet<FeedbackParagraph> FeedbackParagraphs => Set<FeedbackParagraph>();
        public DbSet<Applicant> Applicants => Set<Applicant>();
        public DbSet<FeedbackSession> FeedbackSessions => Set<FeedbackSession>();
        public DbSet<FeedbackResponse> FeedbackResponses => Set<FeedbackResponse>();
        public DbSet<CompletedFeedback> CompletedFeedbacks => Set<CompletedFeedback>();
        public DbSet<HealthInformation> HealthInformation => Set<HealthInformation>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("SensoreDbContext: OnConfiguring called");
                    
                    AppConfiguration? config = null;
                    try
                    {
                        config = AppConfiguration.Instance;
                    }
                    catch (Exception configEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"SensoreDbContext: Failed to get config instance: {configEx.Message}");
                        throw new InvalidOperationException($"Failed to load application configuration: {configEx.Message}. Check appsettings.json file exists and is valid.", configEx);
                    }
                    
                    if (config == null)
                    {
                        throw new InvalidOperationException("Application configuration instance is null. Check appsettings.json file.");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"SensoreDbContext: Got config instance: {config != null}");
                    
                    string? connectionString = null;
                    try
                    {
                        connectionString = config!.ConnectionString;
                    }
                    catch (Exception connEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"SensoreDbContext: Failed to get connection string: {connEx.Message}");
                        throw new InvalidOperationException($"Failed to get connection string: {connEx.Message}. Check ConnectionStrings:DefaultConnection in appsettings.json", connEx);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"SensoreDbContext: Connection string: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");
                    
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException("Connection string is null or empty. Check ConnectionStrings:DefaultConnection in appsettings.json");
                    }
                    
                    optionsBuilder.UseSqlServer(connectionString);
                    System.Diagnostics.Debug.WriteLine("SensoreDbContext: UseSqlServer configured");
                    optionsBuilder.EnableSensitiveDataLogging();
                    optionsBuilder.EnableDetailedErrors();
                    
                    System.Diagnostics.Debug.WriteLine("SensoreDbContext: Configuration complete");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SensoreDbContext OnConfiguring Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                    Logger.Instance?.LogError("Failed to configure database context", ex, "SensoreDbContext");
                    throw new InvalidOperationException($"Database configuration failed: {ex.Message}. Check appsettings.json", ex);
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.UserType).HasMaxLength(20);
                
                entity.HasOne(e => e.AssignedClinician)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedClinicianId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<PressureMapData>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.RecordedDateTime });
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Reviewer)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasOne(e => e.PressureMapData)
                    .WithMany()
                    .HasForeignKey(e => e.DataId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
                    
                entity.HasOne(e => e.Acknowledger)
                    .WithMany()
                    .HasForeignKey(e => e.AcknowledgedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasOne(e => e.PressureMapData)
                    .WithMany()
                    .HasForeignKey(e => e.DataId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
                    
                entity.HasOne(e => e.ParentComment)
                    .WithMany()
                    .HasForeignKey(e => e.ParentCommentId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<MetricsSummary>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.SummaryDate, e.SummaryHour }).IsUnique();
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Template>(entity =>
            {
                entity.HasIndex(e => e.TemplateName);
                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<Section>(entity =>
            {
                entity.HasIndex(e => e.SectionName);
                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<TemplateSectionLink>(entity =>
            {
                entity.HasIndex(e => new { e.TemplateId, e.SectionId }).IsUnique();
                entity.HasIndex(e => new { e.TemplateId, e.DisplayOrder });
                
                entity.HasOne(e => e.Template)
                    .WithMany()
                    .HasForeignKey(e => e.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Section)
                    .WithMany()
                    .HasForeignKey(e => e.SectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Code>(entity =>
            {
                entity.HasIndex(e => new { e.SectionId, e.DisplayOrder });
                
                entity.HasOne(e => e.Section)
                    .WithMany()
                    .HasForeignKey(e => e.SectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<FeedbackParagraph>(entity =>
            {
                entity.HasIndex(e => e.Category);
                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<Applicant>(entity =>
            {
                entity.HasIndex(e => e.SessionUserId);
                entity.HasOne(e => e.SessionUser)
                    .WithMany()
                    .HasForeignKey(e => e.SessionUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<FeedbackSession>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.Status });
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
                    
                entity.HasOne(e => e.Applicant)
                    .WithMany()
                    .HasForeignKey(e => e.ApplicantId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Template)
                    .WithMany()
                    .HasForeignKey(e => e.TemplateId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<FeedbackResponse>(entity =>
            {
                entity.HasIndex(e => e.SessionId);
                
                entity.HasOne(e => e.Session)
                    .WithMany()
                    .HasForeignKey(e => e.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Section)
                    .WithMany()
                    .HasForeignKey(e => e.SectionId)
                    .OnDelete(DeleteBehavior.NoAction);
                    
                entity.HasOne(e => e.Code)
                    .WithMany()
                    .HasForeignKey(e => e.CodeId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<CompletedFeedback>(entity =>
            {
                entity.HasIndex(e => e.CreatedDate);
                entity.HasOne(e => e.Session)
                    .WithMany()
                    .HasForeignKey(e => e.SessionId)
                    .OnDelete(DeleteBehavior.NoAction);
                    
                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<HealthInformation>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.RecordDate });
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
