using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data.Entities;

namespace MovieRaterApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<TvSeries> TvSeries => Set<TvSeries>();
    public DbSet<TvSeason> TvSeasons => Set<TvSeason>();
    public DbSet<TvEpisode> TvEpisodes => Set<TvEpisode>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MediaGenre> MediaGenres => Set<MediaGenre>();
    public DbSet<WatchSession> WatchSessions => Set<WatchSession>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<UserMedia> UserMedias => Set<UserMedia>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<AiSummary> AiSummaries => Set<AiSummary>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<MovieList> MovieLists => Set<MovieList>();
    public DbSet<MediaListMedia> MediaListMedias => Set<MediaListMedia>();
    public DbSet<MovieListGroup> MovieListGroups => Set<MovieListGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity
                .HasOne(e => e.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.User)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Media>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity
                .HasDiscriminator(e => e.MediaType)
                .HasValue<Movie>(MediaType.Movie)
                .HasValue<TvSeries>(MediaType.TvSeries)
                .HasValue<TvSeason>(MediaType.TvSeason)
                .HasValue<TvEpisode>(MediaType.TvEpisode);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PosterUrl).HasMaxLength(500);
            entity.Property(e => e.BackdropUrl).HasMaxLength(500);
            entity.HasIndex(e => new { e.MediaType, e.TmdbId }).IsUnique();
        });

        modelBuilder.Entity<TvSeries>(entity =>
        {
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        modelBuilder.Entity<TvSeason>(entity =>
        {
            entity
                .HasOne(e => e.Series)
                .WithMany(s => s.Seasons)
                .HasForeignKey(e => e.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TvEpisode>(entity =>
        {
            entity
                .HasOne(e => e.Season)
                .WithMany(s => s.Episodes)
                .HasForeignKey(e => e.SeasonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.TmdbId).IsUnique();
        });

        modelBuilder.Entity<MediaGenre>(entity =>
        {
            entity.HasKey(e => new { e.MediaId, e.GenreId });
            entity
                .HasOne(e => e.Media)
                .WithMany(m => m.MediaGenres)
                .HasForeignKey(e => e.MediaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.Genre)
                .WithMany(g => g.MediaGenres)
                .HasForeignKey(e => e.GenreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WatchSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity
                .HasOne(e => e.Group)
                .WithMany(c => c.WatchSessions)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(e => e.Media)
                .WithMany(m => m.WatchSessions)
                .HasForeignKey(e => e.MediaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedWatchSessions)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RatingValue).IsRequired();
            entity.Property(e => e.Review).HasMaxLength(5000);
            entity.HasIndex(e => new { e.WatchSessionId, e.UserId }).IsUnique();
            entity
                .HasOne(e => e.WatchSession)
                .WithMany(ws => ws.Ratings)
                .HasForeignKey(e => e.WatchSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserMedia>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.MediaId });
            entity
                .HasOne(e => e.User)
                .WithMany(u => u.UserMedias)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.Media)
                .WithMany(m => m.UserMedias)
                .HasForeignKey(e => e.MediaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Icon).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.AchievementId });
            entity
                .HasOne(e => e.User)
                .WithMany(u => u.UserAchievements)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.Achievement)
                .WithMany(a => a.UserAchievements)
                .HasForeignKey(e => e.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiSummary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Summary).IsRequired();
            entity
                .HasOne(e => e.WatchSession)
                .WithOne(ws => ws.AiSummary)
                .HasForeignKey<AiSummary>(e => e.WatchSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DeviceInfo).HasMaxLength(200);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InviteeEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.InviteTokenHash).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => e.InviteTokenHash).IsUnique();
            entity.HasIndex(e => e.InviteeEmail);
            entity
                .HasOne(e => e.InviterUser)
                .WithMany()
                .HasForeignKey(e => e.InviterUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.AcceptedByUser)
                .WithMany()
                .HasForeignKey(e => e.AcceptedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MovieList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.OwnerUserId);
            entity
                .HasOne(e => e.OwnerUser)
                .WithMany(u => u.MovieLists)
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaListMedia>(entity =>
        {
            entity.HasKey(e => new { e.MediaListId, e.MediaId });
            entity
                .HasOne(e => e.MediaList)
                .WithMany(l => l.MediaItems)
                .HasForeignKey(e => e.MediaListId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.Media)
                .WithMany(m => m.MediaListMedias)
                .HasForeignKey(e => e.MediaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MovieListGroup>(entity =>
        {
            entity.HasKey(e => new { e.MovieListId, e.GroupId });
            entity
                .HasOne(e => e.MovieList)
                .WithMany(l => l.SharedGroups)
                .HasForeignKey(e => e.MovieListId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.Group)
                .WithMany(g => g.MovieListGroups)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}