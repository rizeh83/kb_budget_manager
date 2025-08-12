using System.Text.Json.Serialization;

namespace KbBudgetManager.Models;

public sealed record LoginResponse(
    [property: JsonPropertyName("u")] UserDto User,
    [property: JsonPropertyName("srvl")] LeagueDto[] Leagues,
    [property: JsonPropertyName("tkn")] string Token,
    [property: JsonPropertyName("tknex")] DateTime TokenExpiresUtc
);

public sealed record UserDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

public sealed record LeagueDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("creatorId")] string CreatorId
);

public sealed record MeLeagueDto(
    [property: JsonPropertyName("b")] decimal B,
    [property: JsonPropertyName("bs")] int Bs,
    [property: JsonPropertyName("mppu")] int Mppu
);

sealed record SquadDto(
    [property: JsonPropertyName("it")] List<SquadPlayer> It,
    [property: JsonPropertyName("mppu")] int Mppu
);
sealed record SquadPlayer(
    [property: JsonPropertyName("i")] string I,
    [property: JsonPropertyName("n")] string N,
    [property: JsonPropertyName("pos")] int Pos,
    [property: JsonPropertyName("mv")] long Mv,
    [property: JsonPropertyName("st")] int St,
    [property: JsonPropertyName("stl")] List<int>? Stl
);

public sealed record AppState(
    string Token,
    DateTime TokenExpiresAtUtc,
    string UserId,
    string LeagueId,
    DateTime SavedAtUtc);

public sealed record MyElevenDto(
    [property: JsonPropertyName("lp")] List<MyElevenPlayer> Lp,
    [property: JsonPropertyName("nlp")] List<MyElevenPlayer> Nlp
);

public sealed record MyElevenPlayer(
    [property: JsonPropertyName("i")] string I,
    [property: JsonPropertyName("n")] string N
);
