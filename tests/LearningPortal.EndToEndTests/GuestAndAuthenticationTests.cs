using Microsoft.Playwright;

namespace LearningPortal.EndToEndTests;

public sealed class GuestAndAuthenticationTests
{
    [Fact]
    public async Task Guest_navigation_preserves_return_url_and_public_routes_render()
    {
        await using var browser = await PortalBrowser.CreateAsync();
        await using var portal = await browser.NewPageAsync();
        portal.ObserveConsole();

        await portal.Page.GotoAsync("/");
        await Expect(portal.Page.GetByTestId("guest-sign-in")).ToBeVisibleAsync();
        await Expect(portal.Page.GetByTestId("register-link").First).ToBeVisibleAsync();

        await portal.Page.GotoAsync("/my-learning");
        await portal.Page.WaitForURLAsync("**/access-denied**");
        await Expect(portal.Page.GetByRole(AriaRole.Heading, new() { Name = "Sign in required" }))
            .ToBeVisibleAsync();
        await portal.Page.GetByTestId("access-sign-in").ClickAsync();
        await Expect(portal.Page).ToHaveURLAsync(new Regex(
            "/login\\?returnUrl=%2Fmy-learning",
            RegexOptions.IgnoreCase));

        portal.AssertNoConsoleErrors();
    }

    [Fact]
    public async Task Student_session_restores_after_refresh_and_logout_blocks_protected_page()
    {
        await using var browser = await PortalBrowser.CreateAsync();
        await using var portal = await browser.NewPageAsync();
        portal.ObserveConsole();

        await portal.SignInAsync(
            portal.Settings.StudentEmail,
            portal.Settings.StudentPassword);
        await Expect(portal.Page.GetByTestId("account-menu")).ToBeVisibleAsync();
        await portal.Page.ReloadAsync();
        await Expect(portal.Page.GetByTestId("account-menu")).ToBeVisibleAsync();

        await portal.Page.GetByTestId("account-menu").ClickAsync();
        await portal.Page.GetByTestId("logout-button").ClickAsync();
        await Expect(portal.Page).ToHaveURLAsync(new Regex("/login\\?signedOut=true"));
        await portal.Page.GotoAsync("/my-learning");
        await Expect(portal.Page.GetByRole(AriaRole.Heading, new() { Name = "Sign in required" }))
            .ToBeVisibleAsync();

        portal.AssertNoConsoleErrors();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

    private static IPageAssertions Expect(IPage page) =>
        Assertions.Expect(page);
}
