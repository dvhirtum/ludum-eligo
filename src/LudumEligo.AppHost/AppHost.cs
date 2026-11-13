var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("ludumeligo");

// Google OAuth client. Set with `aspire secret set Parameters:google-client-id <value>` (see README).
var googleClientId = builder.AddParameter("google-client-id")
    .WithDescription("OAuth client id from the Google Cloud console.");
var googleClientSecret = builder.AddParameter("google-client-secret", secret: true)
    .WithDescription("OAuth client secret from the Google Cloud console.");

// BGG XML API2 application token. Set with `aspire secret set Parameters:bgg-token <value>` (see README).
var bggToken = builder.AddParameter("bgg-token", secret: true)
    .WithDescription("Application token registered at BoardGameGeek for the XML API2.");

var api = builder.AddProject<Projects.LudumEligo_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Authentication__Google__ClientId", googleClientId)
    .WithEnvironment("Authentication__Google__ClientSecret", googleClientSecret)
    .WithEnvironment("Bgg__Token", bggToken)
    .WithHttpHealthCheck("/api/health");

// The SPA is served on a pinned port so the Google redirect URI stays stable.
// The certificate APIs are experimental in Aspire 13; they are only used for local development.
#pragma warning disable ASPIRECERTIFICATES001
builder.AddJavaScriptApp("web", "../ludum-eligo-web", "start:aspire")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpsEndpoint(port: 4200, env: "PORT", isProxied: false)
    .WithHttpsDeveloperCertificate()
    .WithHttpsCertificateConfiguration(context =>
    {
        context.EnvironmentVariables["SSL_CERT_PATH"] = context.CertificatePath;
        context.EnvironmentVariables["SSL_KEY_PATH"] = context.KeyPath;
        return Task.CompletedTask;
    })
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/api/health");
#pragma warning restore ASPIRECERTIFICATES001

builder.Build().Run();
