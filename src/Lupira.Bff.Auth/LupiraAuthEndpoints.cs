using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lupira.Bff.Auth;

/// <summary>The auth surface the SPA drives: sign-in challenge, sign-out, and the current-user probe.</summary>
public static class LupiraAuthEndpoints
{
    public static RouteGroupBuilder MapLupiraAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var options = app.ServiceProvider.GetRequiredService<LupiraBffAuthOptions>();
        var oidc = options.EnableOidc && app.ServiceProvider.GetRequiredService<IHostEnvironment>().IsProduction();
        var group = app.MapGroup("/auth");

        group.MapGet("/login", Results<ChallengeHttpResult, RedirectHttpResult> (string? returnUrl) =>
            {
                var target = SafeReturnUrl(returnUrl);
                return oidc
                    ? TypedResults.Challenge(
                        new AuthenticationProperties { RedirectUri = target },
                        [OpenIdConnectDefaults.AuthenticationScheme])
                    : TypedResults.Redirect(target);
            })
            .AllowAnonymous()
            .ExcludeFromDescription();

        // An expired session must still reach sign-out rather than be challenged into Authentik.
        group.MapPost("/logout", Results<SignOutHttpResult, RedirectHttpResult> () =>
                oidc
                    ? TypedResults.SignOut(
                        new AuthenticationProperties { RedirectUri = "/" },
                        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme])
                    : TypedResults.Redirect("/"))
            .AllowAnonymous()
            .ExcludeFromDescription();

        group.MapGet("/user", Results<Ok<UserInfo>, UnauthorizedHttpResult> (ClaimsPrincipal user) =>
            {
                if (user.Identity?.IsAuthenticated != true)
                    return TypedResults.Unauthorized();

                return TypedResults.Ok(new UserInfo
                {
                    Email = user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                    Name = user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name),
                    Groups = user.FindAll("groups").Select(c => c.Value).ToArray(),
                    IsAdmin = options.IsAdmin(user),
                });
            })
            // Named so the generated client's symbol is stable rather than derived from the path.
            .WithName("GetSession")
            .AllowAnonymous();

        return group;
    }

    // Only allow same-site relative redirects back into the SPA.
    internal static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl)
        && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
        && returnUrl.StartsWith('/')
        && !returnUrl.StartsWith("//", StringComparison.Ordinal)
        && !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
        && !returnUrl.Any(char.IsControl)
            ? returnUrl
            : "/";
}
