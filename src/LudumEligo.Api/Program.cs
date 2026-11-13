using FluentValidation;
using LudumEligo.Api.Common;
using LudumEligo.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddPersistence();

builder.Services.AddProblemDetails();
builder.Services.AddHandlersFromAssembly(typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes: true);

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapDefaultEndpoints();

// Unknown API routes are real 404s; every other unknown route is a client-side SPA route.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

await app.RunAsync();

public partial class Program;
