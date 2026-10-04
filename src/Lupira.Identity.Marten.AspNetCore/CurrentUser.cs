using Marten;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Lupira.Identity.Marten.AspNetCore;

/// <summary><see cref="CurrentUser{TPrincipal}"/> over the plain <see cref="Principal"/> document.</summary>
public sealed class CurrentUser(
    IHttpContextAccessor http,
    PrincipalDirectory directory,
    IDocumentSession session,
    IOptions<CurrentUserOptions> options)
    : CurrentUser<Principal>(http, directory, session, options);
