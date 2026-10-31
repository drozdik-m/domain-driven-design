using Microsoft.AspNetCore.Http;

namespace MartinDrozdik.DDD.Web.Redirects;

/// <summary>
/// A <see cref="Redirect"/> with the status code it answers with.
/// </summary>
/// <param name="Redirect">The redirect.</param>
/// <param name="StatusCode">The status code, e.g. <see cref="StatusCodes.Status301MovedPermanently"/>.</param>
internal sealed record RedirectRegistration(Redirect Redirect, int StatusCode);
