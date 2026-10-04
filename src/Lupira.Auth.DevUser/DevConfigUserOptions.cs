using Microsoft.AspNetCore.Authentication;

namespace Lupira.Auth.DevUser;

public sealed class DevConfigUserOptions : AuthenticationSchemeOptions
{
    public const string UserKey = "Dev:User";
    public const string GroupsKey = "Dev:Groups";

    /// <summary>Used when <c>Dev:User</c> is unset.</summary>
    public string DefaultUser { get; set; } = "dev@localhost";

    /// <summary>Used when <c>Dev:Groups</c> is unset.</summary>
    public IList<string> DefaultGroups { get; set; } = [];
}
