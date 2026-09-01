using Microsoft.AspNetCore.Identity;

namespace Tatami.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Academia do usuário (multi-tenant). Null apenas em cadastros pendentes de onboarding.
    /// </summary>
    public Guid? AcademyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
