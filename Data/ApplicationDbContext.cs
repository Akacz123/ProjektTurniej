using Microsoft.EntityFrameworkCore;
using EsportsTournament.API.Models; // <--- Dodaj ten using

namespace EsportsTournament.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<TeamAvatar> TeamAvatars { get; set; } 
        public DbSet<Game> Games { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<TournamentRegistrationIndividual> TournamentRegistrationIndividual { get; set; }
        public DbSet<TournamentRegistrationTeam> TournamentRegistrationsTeam { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchResult> MatchResults { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserReport> UserReports { get; set; }
        public DbSet<PlayerStatistic> PlayerStatistics { get; set; }
        public DbSet<TeamStatistic> TeamStatistics { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<PresetAvatar> PresetAvatars { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Game>()
                .HasIndex(g => g.GameName)
                .IsUnique();

            modelBuilder.Entity<TournamentRegistrationIndividual>()
                .HasIndex(t => new { t.TournamentId, t.UserId })
                .IsUnique();

            modelBuilder.Entity<TournamentRegistrationTeam>()
                .HasIndex(t => new { t.TournamentId, t.TeamId })
                .IsUnique();

            modelBuilder.Entity<PlayerStatistic>()
                .HasIndex(s => new { s.UserId, s.GameId })
                .IsUnique();

            modelBuilder.Entity<TeamStatistic>()
                .HasIndex(s => new { s.TeamId, s.GameId })
                .IsUnique();

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany()
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Addressee)
                .WithMany()
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.RequesterId, f.AddresseeId })
                .IsUnique();
        }
    }
}