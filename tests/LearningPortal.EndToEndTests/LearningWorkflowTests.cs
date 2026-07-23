using Microsoft.Playwright;

namespace LearningPortal.EndToEndTests;

public sealed class LearningWorkflowTests
{
    [Fact]
    public async Task Seeded_student_can_open_catalog_enrollment_and_ai_tutor_workflows()
    {
        await using var browser = await PortalBrowser.CreateAsync();
        await using var portal = await browser.NewPageAsync();
        portal.ObserveConsole();
        await portal.SignInAsync(
            portal.Settings.StudentEmail,
            portal.Settings.StudentPassword);

        await portal.Page.GotoAsync("/catalog");
        await Expect(portal.Page.GetByRole(AriaRole.Heading, new() { Name = "Course Catalog" }))
            .ToBeVisibleAsync();
        await portal.Page.GotoAsync("/my-learning");
        await Expect(portal.Page.GetByRole(AriaRole.Heading, new() { Name = "My Learning" }))
            .ToBeVisibleAsync();
        await portal.Page.GotoAsync("/ai-tutor");
        await Expect(portal.Page.GetByRole(AriaRole.Heading, new() { Name = "AI Tutor" }))
            .ToBeVisibleAsync();

        portal.AssertNoConsoleErrors();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
