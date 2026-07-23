#pragma warning disable CS1591

using LearningPortal.Blazor.Services;
using LearningPortal.Blazor.Components.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Authorization;

public sealed class AuthenticationNavigationTests
{
    [Fact]
    public void UnauthenticatedRedirect_PreservesOriginalLocalUrl()
    {
        var result = AuthenticationNavigation.BuildAccessDeniedUrl(
            isAuthenticated: false,
            "/my-learning?status=InProgress");

        Assert.Equal(
            "/access-denied?reason=unauthenticated&returnUrl=%2Fmy-learning%3Fstatus%3DInProgress",
            result);
        Assert.Equal(
            "/login?returnUrl=%2Fmy-learning%3Fstatus%3DInProgress",
            AuthenticationNavigation.BuildSignInUrl("/my-learning?status=InProgress"));
    }

    [Fact]
    public void ForbiddenRedirect_DoesNotOfferAuthenticationReturnFlow()
    {
        var result = AuthenticationNavigation.BuildAccessDeniedUrl(
            isAuthenticated: true,
            "/admin/users");

        Assert.Equal("/access-denied?reason=forbidden", result);
        Assert.DoesNotContain("returnUrl", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GuestHeader_OnAccessDenied_UsesOriginalReturnUrl()
    {
        var result = AuthenticationNavigation.ResolveCurrentReturnUrl(
            "https://portal.example/",
            "https://portal.example/access-denied?reason=unauthenticated&returnUrl=%2Fmy-learning");

        Assert.Equal("/my-learning", result);
        Assert.Equal(
            "/login?returnUrl=%2Fmy-learning",
            AuthenticationNavigation.BuildSignInUrl(result));
    }

    [Fact]
    public void LoginAndRegisterComponents_ExposeExpectedRoutes()
    {
        Assert.Contains(
            typeof(Login).GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                .Cast<RouteAttribute>(),
            route => route.Template == "/login");
        Assert.Contains(
            typeof(Register).GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                .Cast<RouteAttribute>(),
            route => route.Template == "/register");
    }

    [Fact]
    public async Task BrowserAuthenticationEndpoints_HaveExpectedAuthorization()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHttpClient();
        builder.Services.AddAntiforgery();
        await using var app = builder.Build();
        app.MapPortalAuthenticationEndpoints();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToDictionary(value => value.RoutePattern.RawText!);

        Assert.NotNull(endpoints["/auth/login"].Metadata.GetMetadata<IAllowAnonymous>());
        Assert.NotNull(endpoints["/auth/register"].Metadata.GetMetadata<IAllowAnonymous>());
        Assert.NotNull(endpoints["/auth/logout"].Metadata.GetMetadata<IAuthorizeData>());
    }

    [Theory]
    [InlineData("https://evil.example/steal")]
    [InlineData("//evil.example/steal")]
    [InlineData(@"\evil.example\steal")]
    public void AuthenticationLinks_RejectExternalReturnUrls(string returnUrl)
    {
        Assert.Equal(
            "/login?returnUrl=%2F",
            AuthenticationNavigation.BuildSignInUrl(returnUrl));
        Assert.Equal(
            "/register?returnUrl=%2F",
            AuthenticationNavigation.BuildRegisterUrl(returnUrl));
    }
}
