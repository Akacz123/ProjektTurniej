using EsportsTournament.API.Models;

namespace EsportsTournament.API.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Count() < 2)
            {
                var users = new List<User>
                {
                    new User { Username = "Faker", Email = "faker@t1.com", PasswordHash = "haslofaker", Role = "user" },
                    new User { Username = "Caps", Email = "caps@g2.com", PasswordHash = "haslodobre", Role = "user" },
                    new User { Username = "Jankos", Email = "jankos@heretics.com", PasswordHash = "haslo321", Role = "user" },
                    new User { Username = "S1mple", Email = "s1mple@navi.com", PasswordHash = "mocnehaslo", Role = "user" },
                    new User { Username = "ZywOo", Email = "zywoo@vitality.com", PasswordHash = "zywooessa", Role = "user" },
                    new User { Username = "Niko", Email = "niko@g2.com", PasswordHash = "haslo123", Role = "user" },
                };
                context.Users.AddRange(users);
                context.SaveChanges();
            }

            
            if (!context.Teams.Any())
            {
                var captain = context.Users.First();
                var teams = new List<Team>
                {
                    new Team { TeamName = "T1", CaptainId = captain.UserId, Description = "Legendarna drużyna LoL." },
                    new Team { TeamName = "G2 Esports", CaptainId = captain.UserId, Description = "Samuraje z Europy." },
                    new Team { TeamName = "Fnatic", CaptainId = captain.UserId, Description = "Pomarańczowo-czarni." },
                    new Team { TeamName = "Natus Vincere", CaptainId = captain.UserId, Description = "Mistrzowie CS2." },
                    new Team { TeamName = "Team Liquid", CaptainId = captain.UserId, Description = "Konie z Ameryki." },
                    new Team { TeamName = "Vitality", CaptainId = captain.UserId, Description = "Pszczoły z Francji." }
                };
                context.Teams.AddRange(teams);
                context.SaveChanges();
            }

           
        }
    }
}