# End-to-end testing

The browser suite is in `tests/LearningPortal.EndToEndTests`. It uses Microsoft
Playwright for .NET and stable `data-testid` selectors. The API and Blazor hosts
must already be running.

## One-time browser setup

```powershell
dotnet build tests/LearningPortal.EndToEndTests/LearningPortal.EndToEndTests.csproj
pwsh tests/LearningPortal.EndToEndTests/bin/Debug/net10.0/playwright.ps1 install chromium
```

## Environment and automated run

1. Start SQL Server or LocalDB and apply migrations:

   ```powershell
   dotnet ef database update --project src/LearningPortal.Infrastructure --startup-project src/LearningPortal.Api
   ```

2. In Development, keep `DevelopmentSeed:Enabled` enabled. Seed accounts and
   sample data are idempotent and never run in Production.
3. For AI Tutor coverage, run `ollama serve`, then
   `ollama pull llama3.2:3b`, and confirm the model with `ollama list`.
4. Start the API and Blazor hosts in separate terminals:

   ```powershell
   dotnet run --project src/LearningPortal.Api
   dotnet run --project src/LearningPortal.Blazor
   ```

5. Run the suite:

   ```powershell
   $env:E2E_BLAZOR_BASE_URL = "https://localhost:7080"
   $env:E2E_API_BASE_URL = "https://localhost:7081"
   dotnet test tests/LearningPortal.EndToEndTests/LearningPortal.EndToEndTests.csproj
   ```

Optional settings are `E2E_ADMIN_EMAIL`, `E2E_ADMIN_PASSWORD`,
`E2E_INSTRUCTOR_EMAIL`, `E2E_INSTRUCTOR_PASSWORD`, `E2E_STUDENT_EMAIL`,
`E2E_STUDENT_PASSWORD`, `E2E_HEADLESS`, and `E2E_TIMEOUT_MS`. Traces are written
under the test output's `playwright-traces` directory. Open one with:

```powershell
pwsh tests/LearningPortal.EndToEndTests/bin/Debug/net10.0/playwright.ps1 show-trace <trace.zip>
```

## Manual acceptance scenario

### Phase A: Environment

1. Start SQL Server or LocalDB.
2. Apply all EF migrations.
3. Enable development seed configuration.
4. Start Ollama.
5. Verify `llama3.2:3b` is installed.
6. Start the API.
7. Start the Blazor application.
8. Open the browser developer console.
9. Confirm no startup errors.

### Phase B: Administrator

1. Sign in with `admin@learningportal.local` / `Admin123!`.
2. Confirm the administrator dashboard loads.
3. Open user management and verify administrator, instructor, and student users.
4. Verify instructor eligibility.
5. Create `End-to-End ASP.NET Core Course`.
6. Set its category or skill, description, eligible instructor, and completion
   requirements.
7. Add lessons named Dependency Injection, Middleware Pipeline, Minimal APIs,
   and Authentication and Authorization.
8. Publish every lesson.
9. Create a required quiz with a valid pass score and attempt limit.
10. Add at least three valid questions with choices and explanations.
11. Publish the quiz and course, verify success feedback, then sign out.

### Phase C: Student

1. Sign in with `student@learningportal.local` / `Student123!`.
2. Find the course in Catalog, enroll, and open My Learning.
3. Open Dependency Injection and ask:
   “Explain dependency injection based on this lesson.”
4. Verify the answer is lesson-related and archive the conversation.
5. Complete every lesson.
6. Open the required quiz. If attempts permit, deliberately fail once, verify
   the failure, retry, and pass.
7. Verify course completion.
8. Issue and download the certificate, copy its verification URL, and sign out.

Certificate steps are currently blocked until a certificate domain/API exists
in this repository; a missing route is not a browser-only defect.

### Phase D: Public verification

Open the verification URL while signed out. Verify active status, learner name,
course title, and issue date, and verify email/private IDs are absent.

### Phase E: Administrator revocation

Sign in as administrator, locate the certificate, revoke it with
`End-to-end revocation test.`, sign out, and verify public status is revoked.

### Phase F: Security and authorization

1. As student, open `/users`; verify the forbidden page and absent admin links.
2. Manipulate another conversation or certificate ID; verify the resource is
   forbidden or hidden.
3. Sign out and reopen `/my-learning`; verify sign-in is required and the local
   return URL is preserved.

### Phase G: Browser and responsive checks

At desktop, tablet, and mobile widths verify the sidebar, account menu, forms,
course cards, lesson player, quiz, AI Tutor, and certificate pages. Operate each
with Tab, Shift+Tab, Enter, Space, and Escape. Confirm the browser console has no
unexpected errors.

## Temporary databases

Tests that create SQL databases must use unique names and drop them from
`finally`/fixture disposal. Never target the primary `LearningPortal`
development database. No database is intentionally retained by the E2E suite.
