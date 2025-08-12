using KbBudgetManager.Helper;
using KbBudgetManager.Models;
using System.Net.Http.Json;

namespace KbBudgetManager;

internal class Program
{
    const double sellDiscount = 0.00; // 0.00 = Kickbase-Angebot (Marktwert). Beispiel: 0.02 für 2% Abzug.
    const string? preferredLeagueId = null; // optional: fest pinnen, sonst erste Liga wählen
    
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            var http = new HttpClient { BaseAddress = new Uri("https://api.kickbase.com") };

            var email = Environment.GetEnvironmentVariable("KB_EMAIL", EnvironmentVariableTarget.User);
            var password = Environment.GetEnvironmentVariable("KB_PASSWORD", EnvironmentVariableTarget.User);
            string? preferredLeagueId = null; // falls du eine feste Liga möchtest

            if (email == null || password == null)
            {
                throw new Exception("invalid username or password -> these values have to be defined in env variables (user) KB_EMAIL and KB_PASSWORD");
            }

            var state = await StateHelper.EnsureState(http, email, password, preferredLeagueId);

            var me = await StateHelper.WithAuth(
                http, 
                state,
                call: async () => await http.GetFromJsonAsync<MeLeagueDto>($"/v4/leagues/{state.LeagueId}/me")
                    ?? throw new InvalidOperationException("leagues/{leagueId}/me leer."),
                reLogin: () => StateHelper.Login(http, email, password, preferredLeagueId));

            // Squad (eigene Spieler) holen
            var squad = await StateHelper.WithAuth(http, state,
                call: async () => await http.GetFromJsonAsync<SquadDto>($"/v4/leagues/{state.LeagueId}/squad")
                    ?? throw new InvalidOperationException("leagues/{leagueId}/squad leer."),
                reLogin: () => StateHelper.Login(http, email, password, preferredLeagueId)
            );

            var myEleven = await StateHelper.WithAuth(http, state,
                call: async () => await http.GetFromJsonAsync<MyElevenDto>($"/v4/leagues/{state.LeagueId}/teamcenter/myeleven")
                    ?? throw new InvalidOperationException("teamcenter/myeleven leer."),
                reLogin: () => StateHelper.Login(http, email, password, preferredLeagueId)
            );

            var startingIds = myEleven.Lp
                .Select(p => p.I)
                .ToHashSet();
            var benchIdsFromApi = myEleven.Nlp
                .Select(p => p.I)
                .ToHashSet();

            var bench = squad.It
                .Where(p => benchIdsFromApi.Contains(p.I) || !startingIds.Contains(p.I))
                .GroupBy(p => p.I).Select(g => g.First())
                .ToList();

            decimal sellSum = bench.Sum(p => Math.Floor(p.Mv * (decimal)(1.0 - sellDiscount)));
            decimal newBalance = me.B + sellSum;

            Console.WriteLine($"Kontostand nach Verkauf der Ersatzspieler:        {newBalance:N0} €");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }

    }
}
