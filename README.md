# Stock Portal — WPF Sign-In Screen

This is a **WPF (.NET 8) desktop app** that recreates the Sign-In screen from your
`bjp-portal-login.html` design: the two-tone saffron identity panel with the
animated chakra/lotus graphic on the left, and the sign-in form on the right.

> **Scope note:** Your HTML file is actually a full multi-page portal (Food
> Packets, Assets, Stock Report, Damage Register, Purchase Orders, Purchases,
> Distributions, Vendors, Products — each with its own modals). This first
> deliverable is just the **Sign-In screen**, pixel-matched, wired to SQL
> Server. Once you confirm this looks right, we build the dashboard shell and
> each section next.

## Prerequisites

- **Visual Studio 2022** (Community edition is fine) with the **".NET desktop
  development"** workload installed (this gives you WPF project support).
- **.NET 8 SDK** (Visual Studio installer can add this automatically).
- Access to your **SQL Server** instance.

## Setup

1. **Open the project**
   Double-click `StockPortalApp.csproj` (or open the folder in Visual
   Studio). Visual Studio will restore the NuGet packages
   (`Microsoft.Data.SqlClient`, `System.Configuration.ConfigurationManager`)
   automatically — this needs an internet connection the first time.

2. **Create the database and table**
   Open `schema.sql` in SQL Server Management Studio (SSMS), connect to your
   **local** SQL Server instance, and click Execute. This one script:
   - Creates the `StockPortal` database (if it doesn't already exist)
   - Creates the `Members` table
   - Creates the `dbo.sp_ValidateMemberLogin` stored procedure the app calls to check logins
   - Seeds one trial login: Membership ID `BJP-2024-000001`, password `Trial@123`

   It's safe to re-run any time — nothing gets duplicated or overwritten.

3. **Point the app at your SQL Server**
   Open `App.config` and edit the `StockPortalDb` connection string:
   ```xml
   <add name="StockPortalDb"
        connectionString="Server=YOUR_SERVER;Database=StockPortal;Trusted_Connection=True;TrustServerCertificate=True;"
        providerName="Microsoft.Data.SqlClient" />
   ```
   Use `Trusted_Connection=True` for Windows Authentication, or
   `User Id=...;Password=...` for a SQL login.

4. **Run it**
   Press `F5` in Visual Studio. Sign in with the trial credentials above to
   confirm the DB connection works.

## What's implemented

- Full visual layout: gradient identity panel, rotating chakra watermark,
  animated "blooming" lotus (staggered scale/opacity entrance, just like the
  CSS keyframes), mission copy, and the sign-in form with rounded inputs,
  focus glow, checkbox, "Forgot password?" link, and gradient button.
- Real SQL Server authentication (`Data/DbHelper.cs`) — calls the
  `dbo.sp_ValidateMemberLogin` stored procedure with the Membership ID/mobile
  number and a SHA-256 password hash.
- Inline error message shown on failed login, matching the HTML's
  `#login-error` behavior.

## Known differences from the HTML (and how to close them)

- **Fonts:** The HTML uses Google Fonts *Baloo 2* and *Inter*. Those aren't
  installed on a typical Windows machine, so this app currently falls back to
  *Segoe UI / Segoe UI Semibold*, which is close but not identical. If you
  want an exact match, download the font files and I can embed them as
  application resources (`Resources/Fonts/...`) — just say the word.
- **Password hashing:** `schema.sql` uses plain SHA-256 for simplicity. For
  production, swap this for a salted algorithm like PBKDF2 or BCrypt — happy
  to change `DbHelper.cs` accordingly.
- Successful login currently just shows a confirmation message box — the
  dashboard screen (sidebar + panels) is the next build step.

## Project structure

```
StockPortalApp/
├─ StockPortalApp.csproj
├─ App.xaml / App.xaml.cs        (colors, shared control styles)
├─ MainWindow.xaml / .xaml.cs    (the sign-in screen)
├─ App.config                    (SQL Server connection string)
├─ schema.sql                    (Members table + trial user)
└─ Data/DbHelper.cs              (SQL Server login validation)
```
