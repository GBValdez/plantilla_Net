using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using project.Models;
using project.users;
using Xunit;
namespace PanelBase.Api.Tests;

public class ApiTests : IAsyncLifetime
{
    private readonly ApiFactory factory = new();
    private HttpClient client = null!;
    public async Task InitializeAsync() { client = factory.CreateClient(); await factory.InitializeAsync(); }
    public Task DisposeAsync() { client.Dispose(); factory.Dispose(); return Task.CompletedTask; }
    private async Task<string> LoginAsync(string name = "member") {
        var response = await client.PostAsJsonAsync("/user/login", new { email = name + "@example.com", password = "Strong!Pass123" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }
    private void Bearer(string token) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Theory]
    [InlineData("/user")]
    [InlineData("/rol")]
    [InlineData("/catalogueType")]
    public async Task Administrative_reads_require_authentication(string route) {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
    }
    [Fact]
    public async Task Member_cannot_read_or_edit_users() {
        await factory.AddUserAsync(); Bearer(await LoginAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/user")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/user/member", new { status = true, roles = new[] { "ADMINISTRATOR" } })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/user/member")).StatusCode);
    }
    [Fact]
    public async Task Login_supports_single_role_and_signed_issuer_audience() {
        var user = await factory.AddUserAsync();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(await LoginAsync());
        Assert.Equal("PanelBase.Api", token.Issuer);
        Assert.Contains("PanelBase.Web", token.Audiences);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "userNormal");
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        Assert.True(token.ValidTo < DateTime.UtcNow.AddMinutes(31));
    }
    [Fact]
    public async Task Unconfirmed_user_cannot_login() {
        await factory.AddUserAsync(confirmed: false);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/user/login", new { email = "member@example.com", password = "Strong!Pass123" })).StatusCode);
    }
    [Fact]
    public async Task Repeated_wrong_passwords_lock_the_account() {
        await factory.AddUserAsync();
        for(var i=0;i<5;i++) await client.PostAsJsonAsync("/user/login", new { email = "member@example.com", password = "Wrong!Pass123" });
        var result = await client.PostAsJsonAsync("/user/login", new { email = "member@example.com", password = "Strong!Pass123" });
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
    [Fact]
    public async Task Unknown_and_unconfirmed_accounts_return_same_recovery_response() {
        await factory.AddUserAsync(confirmed: false);
        foreach(var address in new[] { "missing@example.com", "member@example.com" })
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/user/forgotPassword", new { email = address })).StatusCode);
        Assert.Empty(factory.Services.GetRequiredService<RecordingEmail>().Messages);
    }
    [Fact]
    public async Task Malformed_reset_link_returns_bad_request_instead_of_server_error() {
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/user/resetPassword", new { email = "bad", token = "bad", password = "New!Pass123" })).StatusCode);
    }
    [Fact]
    public async Task Admin_filters_and_empty_results_work_without_invalid_pagination() {
        await factory.AddUserAsync("admin", "ADMINISTRATOR"); await factory.AddUserAsync(); Bearer(await LoginAsync("admin"));
        var result = await client.GetFromJsonAsync<JsonElement>("/user?UserNameCont=member&pageSize=10&pageNumber=1");
        Assert.Equal(1, result.GetProperty("total").GetInt32());
        result = await client.GetFromJsonAsync<JsonElement>("/user?EmailCont=missing");
        Assert.Equal(0, result.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/user?pageSize=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/user?pageNumber=0")).StatusCode);
    }
    [Fact]
    public async Task Admin_can_update_member_but_cannot_disable_self() {
        await factory.AddUserAsync("admin", "ADMINISTRATOR"); await factory.AddUserAsync(); Bearer(await LoginAsync("admin"));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/user/member", new { status = false, roles = new[] { "userNormal" } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/user/admin", new { status = false, roles = new[] { "ADMINISTRATOR" } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/user/missing", new { status = true, roles = new[] { "userNormal" } })).StatusCode);
    }
    [Fact]
    public async Task Disabled_account_cannot_use_an_already_issued_token() {
        var member = await factory.AddUserAsync(); var token = await LoginAsync();
        using(var scope = factory.Services.CreateScope()) {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<userEntity>>();
            var user = await users.FindByIdAsync(member.Id); user!.deleteAt = DateTime.UtcNow; await users.UpdateAsync(user);
        }
        Bearer(token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/catalogueType")).StatusCode);
    }
    [Theory]
    [InlineData("OtherIssuer", "PanelBase.Web", false)]
    [InlineData("PanelBase.Api", "OtherAudience", false)]
    [InlineData("PanelBase.Api", "PanelBase.Web", true)]
    public async Task Incorrect_or_expired_tokens_are_rejected(string issuer, string audience, bool expired) {
        var token = new JwtSecurityToken(issuer, audience, [new Claim(ClaimTypes.Role, "ADMINISTRATOR")],
            expires: expired ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.Key)), SecurityAlgorithms.HmacSha256));
        Bearer(new JwtSecurityTokenHandler().WriteToken(token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/user")).StatusCode);
    }
    private static object Registration() => new { userName = "newuser", email = "newuser@example.com", password = "Strong!Pass123", name = "New User", birthdate = "1990-01-01", tel = "12345678", address = "Test address" };
    [Fact]
    public async Task Registration_confirmation_and_recovery_complete_without_real_smtp() {
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/user/register", Registration())).StatusCode);
        var mail = factory.Services.GetRequiredService<RecordingEmail>();
        var confirm = WebUtility.HtmlDecode(mail.Messages.Single().message.Split("href='")[1].Split("'")[0]);
        var confirmation = new Uri(confirm);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/user/confirmEmail" + confirmation.Query)).StatusCode);
        await LoginAsync("newuser");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/user/forgotPassword", new { email = "newuser@example.com" })).StatusCode);
        var reset = WebUtility.HtmlDecode(mail.Messages.Last().message.Split("href='")[1].Split("'")[0]);
        var segments = new Uri(reset).Segments;
        var payload = new { email = Uri.UnescapeDataString(segments[^2].TrimEnd('/')), token = segments[^1], password = "Changed!Pass123" };
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/user/resetPassword", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/user/resetPassword", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/user/login", new { email = "newuser@example.com", password = "Changed!Pass123" })).StatusCode);
    }
    [Fact]
    public async Task Failed_email_delivery_rolls_back_registration() {
        factory.Services.GetRequiredService<RecordingEmail>().FailDelivery = true;
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PostAsJsonAsync("/user/register", Registration())).StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<DBProyContext>().Users.AnyAsync(u => u.Email == "newuser@example.com"));
    }
    [Fact]
    public async Task Auth_endpoints_limit_repeated_attempts() {
        HttpResponseMessage result = null!;
        for(var i=0;i<21;i++) result = await client.PostAsJsonAsync("/user/forgotPassword", new { email = "missing@example.com" });
        Assert.Equal(HttpStatusCode.TooManyRequests, result.StatusCode);
    }
}
