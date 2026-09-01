namespace Tatami.Domain.Enums;

public static class UserRole
{
    public const string Admin = "admin";
    public const string Professor = "professor";
    public const string Aluno = "aluno";

    public static readonly IReadOnlyList<string> All = [Admin, Professor, Aluno];
}
