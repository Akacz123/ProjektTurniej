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

        // Tu rejestrujemy tabele
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }

        // Konfiguracja dodatkowa (np. unikalne pola)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unikalny Username i Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Unikalna nazwa gry
            modelBuilder.Entity<Game>()
                .HasIndex(g => g.GameName)
                .IsUnique();
        }
    }
}