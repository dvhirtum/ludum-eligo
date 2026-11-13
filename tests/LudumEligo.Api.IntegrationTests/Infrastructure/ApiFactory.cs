using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace LudumEligo.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the API in memory against a PostgreSQL Testcontainer shared by all tests in the "api" collection.
/// Requests with the <see cref="TestAuthHandler.UserHeader"/> header act as that user; all others use the real cookie.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestOrCookieScheme = "TestOrCookie";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public ApiFactory()
    {
        // HTTPS so Secure cookies are stored and sent; no redirects so tests see 302s and 401s.
        ClientOptions.BaseAddress = new Uri("https://localhost");
        ClientOptions.AllowAutoRedirect = false;
    }

    public string ConnectionString => _postgres.GetConnectionString();

    /// <summary>A client holding a real session cookie for <paramref name="userId"/>.</summary>
    public async Task<HttpClient> CreateSignedInClientAsync(Guid userId)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"{TestSignInStartupFilter.Path}?userId={userId}");
        response.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>A client acting as <paramref name="userId"/> through the test authentication handler.</summary>
    public HttpClient CreateClientFor(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId.ToString());
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ludumeligo", ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Authentication:Google:ClientId", "test-client-id");
        builder.UseSetting("Authentication:Google:ClientSecret", "test-client-secret");
        builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWwwroot"));

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter, TestSignInStartupFilter>();
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { })
                .AddPolicyScheme(TestOrCookieScheme, TestOrCookieScheme, options =>
                    options.ForwardDefaultSelector = context =>
                        context.Request.Headers.ContainsKey(TestAuthHandler.UserHeader)
                            ? TestAuthHandler.SchemeName
                            : CookieAuthenticationDefaults.AuthenticationScheme);
            services.PostConfigure<AuthenticationOptions>(options =>
                options.DefaultAuthenticateScheme = TestOrCookieScheme);
        });
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
