using Microsoft.AspNetCore.Antiforgery;

namespace LudumEligo.Api.Features.Auth;

/// <summary>
/// Antiforgery in Angular's XSRF mode: GET requests to <c>/api</c> refresh a script-readable
/// <c>XSRF-TOKEN</c> cookie, and every other <c>/api</c> request must echo it in <c>X-XSRF-TOKEN</c>.
/// </summary>
public static class XsrfProtection
{
    public const string CookieName = "XSRF-TOKEN";
    public const string HeaderName = "X-XSRF-TOKEN";

    public static IServiceCollection AddXsrfProtection(this IServiceCollection services) =>
        services.AddAntiforgery(options =>
        {
            options.HeaderName = HeaderName;
            options.Cookie.Name = "__Host-ludumeligo-antiforgery";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

    /// <summary>
    /// Refreshes the <c>XSRF-TOKEN</c> cookie on safe <c>/api</c> requests. Runs after authentication (tokens are
    /// bound to the user) but before authorization, so anonymous requests that end in <c>401</c> still get a token.
    /// </summary>
    public static IApplicationBuilder UseXsrfTokenIssuing(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api") && IsSafeMethod(context.Request.Method))
            {
                var tokens = context.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);
                context.Response.Cookies.Append(CookieName, tokens.RequestToken!, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                });
            }

            await next(context);
        });

    /// <summary>
    /// Rejects unsafe <c>/api</c> requests without a valid <c>X-XSRF-TOKEN</c> header with <c>400</c>.
    /// Runs after authorization, so anonymous requests to protected endpoints get <c>401</c> first.
    /// </summary>
    public static IApplicationBuilder UseXsrfValidation(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api")
                && !IsSafeMethod(context.Request.Method)
                && !await context.RequestServices.GetRequiredService<IAntiforgery>().IsRequestValidAsync(context))
            {
                await Results.Problem(
                    detail: "The XSRF token is missing or invalid.",
                    statusCode: StatusCodes.Status400BadRequest,
                    extensions: new Dictionary<string, object?> { ["code"] = "xsrf_invalid" })
                    .ExecuteAsync(context);
                return;
            }

            await next(context);
        });

    private static bool IsSafeMethod(string method) =>
        HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);
}
