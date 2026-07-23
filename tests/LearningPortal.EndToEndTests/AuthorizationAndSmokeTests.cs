using Microsoft.Playwright;

namespace LearningPortal.EndToEndTests;

public sealed class AuthorizationAndSmokeTests
{
    [Fact]
    public async Task Student_is_forbidden_from_administration_and_has_no_admin_navigation()
    {
        await using var browser = await PortalBrowser.CreateAsync();
        await using var portal = await browser.NewPageAsync();
        portal.ObserveConsole();
        await portal.SignInAsync(
            portal.Settings.StudentEmail,
            portal.Settings.StudentPassword);

        await portal.Page.GotoAsync("/users");
        await Expect(portal.Page.GetByRole(AriaRole.Heading, new() { Name = "You do not have access" }))
            .ToBeVisibleAsync();
        await Expect(portal.Page.GetByRole(AriaRole.Link, new() { Name = "Users" }))
            .ToHaveCountAsync(0);

        portal.AssertNoConsoleErrors();
    }

    [Fact]
    public async Task Administrator_critical_routes_render_without_unhandled_errors()
    {
        await using var browser = await PortalBrowser.CreateAsync();
        await using var portal = await browser.NewPageAsync();
        portal.ObserveConsole();
        await portal.SignInAsync(
            portal.Settings.AdminEmail,
            portal.Settings.AdminPassword);

        foreach (var route in new[]
                 {
                     "/dashboard",
                     "/users",
                     "/courses",
                     "/admin/instructor-eligibility",
                     "/admin/ai-tutor",
                     "/profile"
                 })
        {
            await portal.Page.GotoAsync(route);
            await Expect(portal.Page.Locator("h1").First).ToBeVisibleAsync();
            await Expect(portal.Page.GetByText("Something went wrong")).ToHaveCountAsync(0);
        }

        portal.AssertNoConsoleErrors();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
