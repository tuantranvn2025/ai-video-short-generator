Flow Automator (WPF)

This is a prototype WPF application that embeds WebView2 for interactive login and allows capturing cookies from the embedded WebView. Captured cookies are saved to disk and can be injected into Playwright for automation of the Flow web UI (https://labs.google/fx/fr/tools/flow/).

WARNING & IMPORTANT NOTES
- Automating Google web pages may violate Google's Terms of Service. Proceed at your own risk.
- Cookies provide full access to your authenticated session. Do NOT share your saved cookie file. Store it securely.
- This is a prototype. You must refine the Playwright automation to match Flow's live DOM selectors.

Prerequisites
- Windows with .NET 8 SDK (or adjust TargetFramework)
- Visual Studio 2022/2023 or `dotnet` CLI
- Playwright CLI to install browser engines

Setup
1. From the `desktop/FlowAutomator` folder run:

```powershell
# install Playwright CLI (one-time)
dotnet tool install --global Microsoft.Playwright.CLI

# restore packages and build
dotnet restore

# install Playwright browsers
playwright install

# run the app
dotnet run
```

Usage
1. Click `Open Login (WebView)` to open the Flow page inside the embedded WebView2.
2. Sign in to your Google account inside the embedded view (complete MFA if required).
3. Click `Capture Cookies` to read cookies for `https://labs.google` from WebView2.
4. Click `Save Cookies` to persist them to `%APPDATA%\FlowAutomator\cookies.json`.
5. Click `Start Automation (Playwright)` to launch Playwright, inject cookies, and navigate to the Flow page using the authenticated session. The current prototype will open a Playwright browser and navigate; you must extend automation steps to drive Flow's UI.

Next steps
- Harden cookie storage using DPAPI / Windows Credential Manager.
- Implement robust Playwright selectors for Flow's UI and network interception to obtain final video download URLs.
- Add UI to show download progress and play the downloaded video in the app.

Security
- Keep the cookie file private. Delete it when not needed.
- Consider implementing the automation only for local use and do not upload cookie files to untrusted hosts.
