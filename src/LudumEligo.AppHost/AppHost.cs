var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("ludumeligo");

var api = builder.AddProject<Projects.LudumEligo_Api>("api")
    .WithReference(database)
    .WaitFor(database)
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
