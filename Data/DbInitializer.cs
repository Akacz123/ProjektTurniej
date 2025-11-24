using EsportsTournament.API.Models;

namespace EsportsTournament.API.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Matches.Any())
            {
                context.MatchResults.RemoveRange(context.MatchResults);
                context.Matches.RemoveRange(context.Matches);
                context.SaveChanges();
            }

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

            var existingTeams = context.Teams.ToList();
            var tournaments = context.Tournaments.ToList();
            var adminUser = context.Users.FirstOrDefault();
            var random = new Random();

            if (!existingTeams.Any() || !tournaments.Any()) return;

            var matches = new List<Match>();

            foreach (var tournament in tournaments)
            {
                for (int i = 1; i <= 5; i++) 
                {
                  
                    var team1 = existingTeams[random.Next(existingTeams.Count)];
                    var team2 = existingTeams[random.Next(existingTeams.Count)];

                
                    while (team1.TeamId == team2.TeamId)
                    {
                        team2 = existingTeams[random.Next(existingTeams.Count)];
                    }

                    var match = new Match
                    {
                        TournamentId = tournament.TournamentId,
                        RoundNumber = 1,
                        MatchNumber = i,
                        Participant1Type = "team",
                        Participant2Type = "team",
        
                        Participant1Id = team1.TeamId,
                        Participant2Id = team2.TeamId,
                        ScheduledTime = DateTime.UtcNow.AddDays(random.Next(-5, 10)),
                        MatchStatus = "completed"
                    };
                    matches.Add(match);
                }
            }

            context.Matches.AddRange(matches);
            context.SaveChanges();

            var results = new List<MatchResult>();
            foreach (var match in matches)
            {
                results.Add(new MatchResult
                {
                    MatchId = match.MatchId,
                    Participant1Score = random.Next(0, 3), 
                    Participant2Score = random.Next(0, 3),
                    ReportedBy = adminUser != null ? adminUser.UserId : 1,
                    ResultStatus = "confirmed",
                    ReportedAt = DateTime.UtcNow
                });
            }
            context.MatchResults.AddRange(results);
            context.SaveChanges();
        }
    }
}