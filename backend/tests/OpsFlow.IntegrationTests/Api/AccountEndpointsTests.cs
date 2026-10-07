using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Features.Auth;
using OpsFlow.Domain.Exceptions;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class AccountEndpointsTests
{
    private const string NewPassword = "New-password-2";

    private static readonly TimeSpan PastCooldown = TimeSpan.FromMinutes(5);

    private readonly OpsFlowApiFactory _factory;
    private readonly HttpClient _client;

    public AccountEndpointsTests(OpsFlowApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Register_SendsAVerificationLinkToTheNewUser()
    {
        var registration = AuthClientExtensions.NewRegistration();

        var auth = await _client.RegisterAsync(registration);

        var email = Assert.Single(_factory.Emails.SentTo(registration.Email));

        Assert.Contains($"{OpsFlowApiFactory.FrontendBaseUrl}/verify-email#token=", email.TextBody);
        Assert.Contains("/verify-email#token=", email.HtmlBody);
        Assert.False(auth.User.IsEmailVerified);
    }

    [Fact]
    public async Task VerifyEmail_WithTheEmailedToken_MarksTheUserAsVerified()
    {
        var registration = AuthClientExtensions.NewRegistration();
        var auth = await _client.RegisterAsync(registration);
        var token = LatestTokenSentTo(registration.Email);

        var response = await VerifyEmailAsync(token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var me = await _client.GetCurrentUserAsync(auth.AccessToken);
        Assert.True(me.IsEmailVerified);
    }

    [Fact]
    public async Task VerifyEmail_WithTheSameTokenTwice_RejectsTheSecondAttempt()
    {
        var registration = AuthClientExtensions.NewRegistration();
        await _client.RegisterAsync(registration);
        var token = LatestTokenSentTo(registration.Email);

        await VerifyEmailAsync(token);
        var second = await VerifyEmailAsync(token);

        await AssertInvalidTokenAsync(second);
    }

    [Fact]
    public async Task VerifyEmail_WithAnUnknownToken_IsRejected()
    {
        var response = await VerifyEmailAsync("not-a-real-token");

        await AssertInvalidTokenAsync(response);
    }

    [Fact]
    public async Task VerifyEmail_WithAnExpiredToken_IsRejected()
    {
        var registration = AuthClientExtensions.NewRegistration();
        var auth = await _client.RegisterAsync(registration);
        var token = LatestTokenSentTo(registration.Email);

        await _factory.ExpireUserTokensAsync(auth.User.Id);

        var response = await VerifyEmailAsync(token);

        await AssertInvalidTokenAsync(response);

        var me = await _client.GetCurrentUserAsync(auth.AccessToken);
        Assert.False(me.IsEmailVerified);
    }

    [Fact]
    public async Task VerifyEmail_WithAPasswordResetToken_IsRejectedAndLeavesThatTokenUsable()
    {
        var registration = AuthClientExtensions.NewRegistration();
        await _client.RegisterAsync(registration);

        await ForgotPasswordAsync(registration.Email);
        var resetToken = LatestTokenSentTo(registration.Email);

        var verification = await VerifyEmailAsync(resetToken);
        await AssertInvalidTokenAsync(verification);

        var reset = await ResetPasswordAsync(resetToken, NewPassword);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_WithoutAccessToken_Returns401()
    {
        var response = await ResendVerificationAsync(accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_WithinTheCooldown_SendsNothing()
    {
        var registration = AuthClientExtensions.NewRegistration();
        var auth = await _client.RegisterAsync(registration);

        var response = await ResendVerificationAsync(auth.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Single(_factory.Emails.SentTo(registration.Email));
    }

    [Fact]
    public async Task ResendVerification_AfterTheCooldown_SendsANewLinkAndInvalidatesTheOldOne()
    {
        var registration = AuthClientExtensions.NewRegistration();
        var auth = await _client.RegisterAsync(registration);
        var firstToken = LatestTokenSentTo(registration.Email);

        await _factory.BackdateUserTokensAsync(auth.User.Id, PastCooldown);

        var response = await ResendVerificationAsync(auth.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(2, _factory.Emails.SentTo(registration.Email).Count);

        var secondToken = LatestTokenSentTo(registration.Email);
        Assert.NotEqual(firstToken, secondToken);

        await AssertInvalidTokenAsync(await VerifyEmailAsync(firstToken));
        Assert.Equal(HttpStatusCode.NoContent, (await VerifyEmailAsync(secondToken)).StatusCode);
    }

    [Fact]
    public async Task ResendVerification_ForAVerifiedUser_SendsNothing()
    {
        var registration = AuthClientExtensions.NewRegistration();
        var auth = await _client.RegisterAsync(registration);

        await VerifyEmailAsync(LatestTokenSentTo(registration.Email));
        await _factory.BackdateUserTokensAsync(auth.User.Id, PastCooldown);

        var response = await ResendVerificationAsync(auth.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Single(_factory.Emails.SentTo(registration.Email));
    }

    [Fact]
    public async Task ForgotPassword_ForAnUnknownEmail_Returns204AndSendsNothing()
    {
        var email = $"nobody-{Guid.NewGuid():N}@test.local";

        var response = await ForgotPasswordAsync(email);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(_factory.Emails.SentTo(email));
    }

    [Fact]
    public async Task ForgotPassword_WithAMalformedEmail_Returns400()
    {
        var response = await ForgotPasswordAsync("not-an-email");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("email", problem.Errors.Keys);
    }

    [Fact]
    public async Task ForgotPassword_IsCaseInsensitiveAboutTheEmailAddress()
    {
        var registration = AuthClientExtensions.NewRegistration();
        await _client.RegisterAsync(registration);

        var response = await ForgotPasswordAsync(registration.Email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(
            _factory.Emails.SentTo(registration.Email),
            email => email.TextBody.Contains("/reset-password#token=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResetPassword_WithTheEmailedToken_ChangesThePasswordAndEndsEverySession()
    {
        var registration = AuthClientExtensions.NewRegistration();
        var auth = await _client.RegisterAsync(registration);

        await ForgotPasswordAsync(registration.Email);
        var token = LatestTokenSentTo(registration.Email);

        var reset = await ResetPasswordAsync(token, NewPassword);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var oldPasswordLogin = await _client.LoginAsync(registration.Email, registration.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

        var oldSession = await _client.RefreshAsync(auth.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);

        var newPasswordLogin = await _client.LoginAsync(registration.Email, NewPassword);
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);

        var signedIn = await newPasswordLogin.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(signedIn);
        Assert.True(signedIn.User.IsEmailVerified);
    }

    [Fact]
    public async Task ResetPassword_WithTheSameTokenTwice_RejectsTheSecondAttempt()
    {
        var registration = AuthClientExtensions.NewRegistration();
        await _client.RegisterAsync(registration);

        await ForgotPasswordAsync(registration.Email);
        var token = LatestTokenSentTo(registration.Email);

        await ResetPasswordAsync(token, NewPassword);
        var second = await ResetPasswordAsync(token, "Another-password-3");

        await AssertInvalidTokenAsync(second);

        var login = await _client.LoginAsync(registration.Email, NewPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithAnExpiredToken_IsRejectedAndKeepsTheOldPassword()
    {
        var registration = AuthClientExtensions.NewRegistration();
        var auth = await _client.RegisterAsync(registration);

        await ForgotPasswordAsync(registration.Email);
        var token = LatestTokenSentTo(registration.Email);

        await _factory.ExpireUserTokensAsync(auth.User.Id);

        var response = await ResetPasswordAsync(token, NewPassword);

        await AssertInvalidTokenAsync(response);

        var login = await _client.LoginAsync(registration.Email, registration.Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithAWeakPassword_Returns400AndLeavesTheTokenUsable()
    {
        var registration = AuthClientExtensions.NewRegistration();
        await _client.RegisterAsync(registration);

        await ForgotPasswordAsync(registration.Email);
        var token = LatestTokenSentTo(registration.Email);

        var weak = await ResetPasswordAsync(token, "short");

        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);

        var problem = await weak.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("newPassword", problem.Errors.Keys);

        var valid = await ResetPasswordAsync(token, NewPassword);
        Assert.Equal(HttpStatusCode.NoContent, valid.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_RequestedAgainAfterTheCooldown_InvalidatesTheEarlierLink()
    {
        var registration = AuthClientExtensions.NewRegistration();
        var auth = await _client.RegisterAsync(registration);

        await ForgotPasswordAsync(registration.Email);
        var firstToken = LatestTokenSentTo(registration.Email);

        await _factory.BackdateUserTokensAsync(auth.User.Id, PastCooldown);

        await ForgotPasswordAsync(registration.Email);
        var secondToken = LatestTokenSentTo(registration.Email);

        Assert.NotEqual(firstToken, secondToken);

        await AssertInvalidTokenAsync(await ResetPasswordAsync(firstToken, NewPassword));
        Assert.Equal(HttpStatusCode.NoContent, (await ResetPasswordAsync(secondToken, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_RequestedAgainWithinTheCooldown_SendsOnlyOneEmail()
    {
        var registration = AuthClientExtensions.NewRegistration();
        await _client.RegisterAsync(registration);

        await ForgotPasswordAsync(registration.Email);
        await ForgotPasswordAsync(registration.Email);

        var resetEmails = _factory.Emails.SentTo(registration.Email)
            .Where(email => email.TextBody.Contains("/reset-password#token=", StringComparison.Ordinal));

        Assert.Single(resetEmails);
    }

    private string LatestTokenSentTo(string address)
    {
        var emails = _factory.Emails.SentTo(address);

        Assert.NotEmpty(emails);

        return CapturingEmailQueue.ExtractToken(emails[^1]);
    }

    private Task<HttpResponseMessage> VerifyEmailAsync(string token) =>
        _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(token));

    private Task<HttpResponseMessage> ResendVerificationAsync(string? accessToken) =>
        _client.SendWithTokenAsync(HttpMethod.Post, "/api/auth/resend-verification", accessToken);

    private Task<HttpResponseMessage> ForgotPasswordAsync(string email) =>
        _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(email));

    private Task<HttpResponseMessage> ResetPasswordAsync(string token, string newPassword) =>
        _client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(token, newPassword));

    private static async Task AssertInvalidTokenAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = await response.ReadProblemAsync();

        Assert.Equal(ErrorCodes.Auth.InvalidToken, problem.Code());
    }
}
