using KbBudgetManager.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace KbBudgetManager.Helper;

public static class StateHelper
{
    private static readonly string StatePath = Path.Combine(AppContext.BaseDirectory, ".kickbase_state.json");

    public static AppState? LoadState()
    {
        if (!File.Exists(StatePath)) return null;
        var json = File.ReadAllText(StatePath);
        return JsonSerializer.Deserialize<AppState>(json);
    }

    public static void SaveState(AppState s)
    {
        var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(StatePath, json);
    }

    public static async Task<AppState> Login(HttpClient http, string email, string password, string? preferredLeagueId = null)
    {
        var payload = new
        {
            em = email,
            pass = password,
            loy = false
        };

        using var res = await http.PostAsJsonAsync("/v4/user/login", payload);
        res.EnsureSuccessStatusCode();

        var login = await res.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Leere Login-Response.");

        if (string.IsNullOrWhiteSpace(login.Token))
            throw new InvalidOperationException("Token fehlt in Login-Response.");

        // Liga wählen: bevorzugte ID oder erste aus srvl
        var league = ((preferredLeagueId is null)
            ? login.Leagues?.FirstOrDefault()
            : login.Leagues?.FirstOrDefault(l => l.Id == preferredLeagueId)) 
            ?? throw new InvalidOperationException("Keine Liga in Login-Response gefunden.");

        var state = new AppState(
            Token: login.Token,
            TokenExpiresAtUtc: login.TokenExpiresUtc,     // tknex ist bereits UTC
            UserId: login.User.Id,
            LeagueId: league.Id,
            SavedAtUtc: DateTime.UtcNow
        );

        SaveState(state);
        return state;
    }

    public static async Task<AppState> EnsureState(HttpClient http, string email, string password, string? preferredLeagueId = null)
    {
        var state = LoadState();
        if (state is null || DateTime.UtcNow >= state.TokenExpiresAtUtc.AddMinutes(-1)) 
            return await Login(http, email, password, preferredLeagueId);

        return state;
    }

    public static async Task<T> WithAuth<T>(
        HttpClient http, AppState state,
        Func<Task<T>> call,
        Func<Task<AppState>> reLogin)
    {
        http.DefaultRequestHeaders.Remove("authorization");
        http.DefaultRequestHeaders.Add("authorization", $"Bearer {state.Token}");

        try
        {
            return await call();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var fresh = await reLogin();
            http.DefaultRequestHeaders.Remove("authorization");
            http.DefaultRequestHeaders.Add("authorization", $"Bearer {fresh.Token}");
            SaveState(fresh);
            return await call();
        }
    }
}
