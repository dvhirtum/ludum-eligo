using System.Net;
using System.Net.Http.Json;
using LudumEligo.Api.Domain;
using LudumEligo.Api.Features.Import;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LudumEligo.Api.IntegrationTests.Features.Import;

[Collection(ApiCollection.Name)]
public sealed class StartImportTests(ApiFactory factory)
{
    private async Task<(User User, HttpClient Client)> SignedInAsync(string? bggUsername)
    {
        var user = await factory.CreateUserAsync(bggUsername: bggUsername);
        return (user, await factory.CreateClientFor(user.Id).WithXsrfTokenAsync());
    }

    [Fact]
    public async Task Should_queue_an_import_When_a_username_is_linked()
    {
        var (user, client) = await SignedInAsync(TestImports.UniqueBggUsername());

        var response = await client.PostAsync("/api/import", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var job = await response.Content.ReadFromJsonAsync<GetLatestImport.ImportJobResponse>();
        Assert.Equal("Queued", job!.Status);
        var stored = await factory.FindImportJobAsync(job.Id);
        Assert.Equal(user.Id, stored!.UserId);
        Assert.Equal(ImportStatus.Queued, stored.Status);
    }

    [Fact]
    public async Task Should_reject_the_import_When_no_username_is_linked()
    {
        var (_, client) = await SignedInAsync(bggUsername: null);

        var response = await client.PostAsync("/api/import", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("bgg_username_required", problem!.Extensions["code"]!.ToString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/import/latest")).StatusCode);
    }

    [Theory]
    [InlineData(ImportStatus.Queued)]
    [InlineData(ImportStatus.Running)]
    public async Task Should_return_the_active_import_When_one_is_already_running(ImportStatus status)
    {
        var (user, client) = await SignedInAsync(TestImports.UniqueBggUsername());
        var active = await factory.CreateImportJobAsync(user.Id, status);

        var response = await client.PostAsync("/api/import", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var job = await response.Content.ReadFromJsonAsync<GetLatestImport.ImportJobResponse>();
        Assert.Equal(active.Id, job!.Id);
        Assert.Equal(status.ToString(), job.Status);
    }

    [Fact]
    public async Task Should_queue_a_new_import_When_the_previous_one_finished()
    {
        var (user, client) = await SignedInAsync(TestImports.UniqueBggUsername());
        var finished = await factory.CreateImportJobAsync(user.Id, ImportStatus.Failed);

        var response = await client.PostAsync("/api/import", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotEqual(finished.Id, (await response.Content.ReadFromJsonAsync<GetLatestImport.ImportJobResponse>())!.Id);
    }

    [Fact]
    public async Task Should_return_the_latest_status_When_requested()
    {
        var (user, client) = await SignedInAsync(TestImports.UniqueBggUsername());
        await factory.CreateImportJobAsync(user.Id, ImportStatus.Succeeded, DateTimeOffset.UtcNow.AddHours(-2));
        var latest = await factory.CreateImportJobAsync(user.Id, ImportStatus.Failed, DateTimeOffset.UtcNow.AddHours(-1));
        var other = await factory.CreateUserAsync();
        await factory.CreateImportJobAsync(other.Id, ImportStatus.Queued);

        var response = await client.GetAsync("/api/import/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var job = await response.Content.ReadFromJsonAsync<GetLatestImport.ImportJobResponse>();
        Assert.Equal(latest.Id, job!.Id);
        Assert.Equal("Failed", job.Status);
    }

    [Fact]
    public async Task Should_return_204_When_no_import_was_started()
    {
        var (_, client) = await SignedInAsync(TestImports.UniqueBggUsername());

        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/import/latest")).StatusCode);
    }
}
