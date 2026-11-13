using FluentValidation;
using LudumEligo.Api.Common;
using LudumEligo.Api.Features.Auth;
using LudumEligo.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddPersistence();
builder.AddAuth();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddXsrfProtection();
builder.Services.AddProblemDetails();
builder.Services.AddHandlersFromAssembly(typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes: true);

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuth();

app.MapDefaultEndpoints();

var auth = app.MapGroup("/api/auth");
auth.MapLogin();
auth.MapGetCurrentUser();
auth.MapSignOut();

// Everything under /api requires a signed-in user, except /api/auth and the health endpoints.
var api = app.MapGroup("/api").RequireAuthorization();

// Unknown API routes are real 404s; every other unknown route is a client-side SPA route.
api.MapFallback("{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

await app.RunAsync();

public partial class Program;
