using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;

namespace LudumEligo.Api.Features.Auth;

/// <summary>Cookie session plus server-side Google OAuth. The browser never sees a token.</summary>
public static class AuthenticationSetup
{
    public const string SessionCookieName = "__Host-ludumeligo-session";
    public const string GoogleCallbackPath = "/api/auth/signin-google";
    public const string SignInFailedRedirect = "/?signInFailed=true";

    public static IHostApplicationBuilder AddAuth(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context => StatusInsteadOfRedirect(context, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context => StatusInsteadOfRedirect(context, StatusCodes.Status403Forbidden);
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
                options.CallbackPath = GoogleCallbackPath;
                options.Events.OnTicketReceived = SignInWithGoogle.OnTicketReceived;
                options.Events.OnRemoteFailure = context =>
                {
                    context.Response.Redirect(SignInFailedRedirect);
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });

        builder.Services.AddAuthorization();

        if (builder.Environment.IsDevelopment())
        {
            // The Angular dev proxy forwards to the API; trust its X-Forwarded-* headers so the
            // Google redirect URI is built with the SPA's origin (https://localhost:4200).
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        return builder;
    }

    public static WebApplication UseAuth(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseForwardedHeaders();
        }

        app.UseAuthentication();
        app.UseXsrfTokenIssuing();
        app.UseAuthorization();
        app.UseXsrfValidation();
        return app;
    }

    private static Task StatusInsteadOfRedirect(Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
