namespace LearningPortal.Blazor.Services;

/// <summary>
/// Builds safe local navigation URLs for authentication and authorization outcomes.
/// </summary>
public static class AuthenticationNavigation
{
    /// <summary>Gets the existing interactive sign-in route.</summary>
    public const string SignInRoute = "/login";

    /// <summary>Gets the existing interactive registration route.</summary>
    public const string RegisterRoute = "/register";

    /// <summary>Builds the access-denied route for the current authentication state.</summary>
    public static string BuildAccessDeniedUrl(bool isAuthenticated, string? returnUrl)
    {
        var reason = isAuthenticated ? "forbidden" : "unauthenticated";
        var safeReturnUrl = NormalizeLocalReturnUrl(returnUrl);
        return isAuthenticated
            ? $"/access-denied?reason={reason}"
            : $"/access-denied?reason={reason}&returnUrl={Uri.EscapeDataString(safeReturnUrl)}";
    }

    /// <summary>Builds a sign-in URL that returns to a validated local portal path.</summary>
    public static string BuildSignInUrl(string? returnUrl) =>
        BuildAuthenticationUrl(SignInRoute, returnUrl);

    /// <summary>Builds a registration URL that returns to a validated local portal path.</summary>
    public static string BuildRegisterUrl(string? returnUrl) =>
        BuildAuthenticationUrl(RegisterRoute, returnUrl);

    /// <summary>
    /// Resolves the best local return target for the current page, including an
    /// original target carried by the unauthenticated access-denied page.
    /// </summary>
    public static string ResolveCurrentReturnUrl(string baseUri, string currentUri)
    {
        var root = new Uri(baseUri, UriKind.Absolute);
        var current = new Uri(currentUri, UriKind.Absolute);
        var relative = root.MakeRelativeUri(current).ToString();
        var decoded = Uri.UnescapeDataString(relative);
        var currentPath = string.IsNullOrWhiteSpace(decoded) ? "/" : $"/{decoded}";

        if (!currentPath.StartsWith("/access-denied?", StringComparison.OrdinalIgnoreCase))
        {
            return NormalizeLocalReturnUrl(currentPath);
        }

        var query = current.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Split('=', 2))
            .FirstOrDefault(value =>
                value.Length == 2
                && string.Equals(value[0], "returnUrl", StringComparison.OrdinalIgnoreCase));
        return query is null
            ? "/"
            : NormalizeLocalReturnUrl(Uri.UnescapeDataString(query[1]));
    }

    /// <summary>
    /// Normalizes a return target to a local root-relative path to prevent open redirects.
    /// </summary>
    public static string NormalizeLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        var candidate = returnUrl.Trim();
        if (!candidate.StartsWith("/", StringComparison.Ordinal)
            || candidate.StartsWith("//", StringComparison.Ordinal)
            || candidate.Contains('\\')
            || Uri.TryCreate(candidate, UriKind.Absolute, out _))
        {
            return "/";
        }

        return candidate;
    }

    private static string BuildAuthenticationUrl(string route, string? returnUrl)
    {
        var safeReturnUrl = NormalizeLocalReturnUrl(returnUrl);
        return $"{route}?returnUrl={Uri.EscapeDataString(safeReturnUrl)}";
    }
}
