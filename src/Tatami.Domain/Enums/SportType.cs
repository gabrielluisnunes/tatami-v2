namespace Tatami.Domain.Enums;

public enum SportType
{
    JiuJitsu,
    MuayThai,
    Boxe,
    Misto,
}

public static class SportTypeExtensions
{
    public static string ToSlug(this SportType sport) => sport switch
    {
        SportType.JiuJitsu => "jiu-jitsu",
        SportType.MuayThai => "muay thai",
        SportType.Boxe => "boxe",
        SportType.Misto => "misto",
        _ => throw new ArgumentOutOfRangeException(nameof(sport)),
    };

    public static SportType FromSlug(string slug) => slug.ToLowerInvariant() switch
    {
        "jiu-jitsu" => SportType.JiuJitsu,
        "muay thai" => SportType.MuayThai,
        "boxe" => SportType.Boxe,
        "misto" => SportType.Misto,
        _ => throw new ArgumentException($"Esporte inválido: {slug}"),
    };

    public static readonly IReadOnlyList<string> AllSlugs =
        ["jiu-jitsu", "muay thai", "boxe", "misto"];
}
