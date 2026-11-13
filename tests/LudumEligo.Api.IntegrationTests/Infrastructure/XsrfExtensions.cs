namespace LudumEligo.Api.IntegrationTests.Infrastructure;

public static class XsrfExtensions
{
    public const string CookieName = "XSRF-TOKEN";
    public const string HeaderName = "X-XSRF-TOKEN";

    /// <summary>Does what Angular does: reads the XSRF-TOKEN cookie from a GET and echoes it in a header.</summary>
    public static async Task<HttpClient> WithXsrfTokenAsync(this HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/me");
        var token = ReadXsrfCookie(response) ?? throw new InvalidOperationException("No XSRF-TOKEN cookie was issued.");
        client.DefaultRequestHeaders.Remove(HeaderName);
        client.DefaultRequestHeaders.Add(HeaderName, token);
        return client;
    }

    public static string? ReadXsrfCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies
                .Where(c => c.StartsWith(CookieName + "="))
                .Select(c => Uri.UnescapeDataString(c[(CookieName.Length + 1)..].Split(';')[0]))
                .FirstOrDefault()
            : null;
}
