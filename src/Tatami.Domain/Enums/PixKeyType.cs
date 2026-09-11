namespace Tatami.Domain.Enums;

public enum PixKeyType
{
    Celular,
    Email,
    Cpf,
    Cnpj,
    Aleatoria,
}

public static class PixKeyTypeExtensions
{
    public static string ToSlug(this PixKeyType type) => type switch
    {
        PixKeyType.Celular => "celular",
        PixKeyType.Email => "email",
        PixKeyType.Cpf => "cpf",
        PixKeyType.Cnpj => "cnpj",
        PixKeyType.Aleatoria => "aleatoria",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static PixKeyType FromSlug(string slug) => slug.Trim().ToLowerInvariant() switch
    {
        "celular" => PixKeyType.Celular,
        "email" => PixKeyType.Email,
        "cpf" => PixKeyType.Cpf,
        "cnpj" => PixKeyType.Cnpj,
        "aleatoria" => PixKeyType.Aleatoria,
        _ => throw new ArgumentException($"Tipo de chave PIX inválido: {slug}"),
    };

    public static readonly IReadOnlyList<string> AllSlugs =
        ["celular", "email", "cpf", "cnpj", "aleatoria"];
}