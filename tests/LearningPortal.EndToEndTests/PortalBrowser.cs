using Microsoft.Playwright;

namespace LearningPortal.EndToEndTests;

internal sealed class PortalBrowser : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly E2eSettings _settings;

    private PortalBrowser(
        IPlaywright playwright,
        IBrowser browser,
        E2eSettings settings)
    {
        _playwright = playwright;
        _browser = browser;
        _settings = settings;
    }

    public static async Task<PortalBrowser> CreateAsync()
    {
        var settings = E2eSettings.Load();
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = settings.Headless
            });
        return new PortalBrowser(playwright, browser, settings);
    }

    public async Task<PortalPage> NewPageAsync(bool ignoreHttpsErrors = true)
    {
        var context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = ignoreHttpsErrors,
                BaseURL = _settings.BlazorBaseUrl
            });
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(_settings.TimeoutMilliseconds);
        return new PortalPage(context, page, _settings);
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}

internal sealed class PortalPage(
    IBrowserContext context,
    IPage page,
    E2eSettings settings) : IAsyncDisposable
{
    private readonly List<string> _consoleErrors = [];

    public IPage Page { get; } = page;
    public E2eSettings Settings { get; } = settings;

    public void ObserveConsole()
    {
        Page.Console += (_, message) =>
        {
            if (message.Type == "error"
                && !message.Text.Contains("favicon", StringComparison.OrdinalIgnoreCase))
            {
                _consoleErrors.Add(message.Text);
            }
        };
        Page.PageError += (_, message) => _consoleErrors.Add(message);
    }

    public async Task SignInAsync(string email, string password, string returnUrl = "/dashboard")
    {
        await Page.GotoAsync($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        await Page.GetByTestId("login-email").FillAsync(email);
        await Page.GetByTestId("login-password").FillAsync(password);
        await Page.GetByTestId("login-submit").ClickAsync();
        await Page.WaitForURLAsync($"**{returnUrl}");
    }

    public void AssertNoConsoleErrors() =>
        Assert.True(
            _consoleErrors.Count == 0,
            $"Unexpected browser console errors:{Environment.NewLine}{string.Join(Environment.NewLine, _consoleErrors)}");

    public async ValueTask DisposeAsync()
    {
        var traceDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "playwright-traces");
        Directory.CreateDirectory(traceDirectory);
        var traceName = $"trace-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.zip";
        await context.Tracing.StopAsync(
            new() { Path = Path.Combine(traceDirectory, traceName) });
        await context.DisposeAsync();
    }
}
