# Target Architecture - ASP.NET Core Blazor Server

## Framework

- ASP.NET Core 10.0 with Blazor Server
- Entity Framework Core 10.0 with SQLite provider (preserving the existing Orders.db)

## UI Mapping

- VB6 MDI Container (frmMain.frm) → Blazor MainLayout.razor with NavMenu component
- VB6 Child Forms → Individual Blazor page components under /Pages
- VB6 MSFlexGrid → HTML table with Blazor data binding
- VB6 Toolbar/ImageList → Blazor NavMenu with icons
- VB6 ADODC data-bound controls → Blazor @bind with EF Core backing

## Data Layer

- Replace ADO/ADODC with Entity Framework Core
- Generate DbContext with entity models for each table in Orders.db
- All queries must use async/await (ToListAsync, FirstOrDefaultAsync)
- Use IDbContextFactory for Blazor Server thread safety

## Authentication

- Convert frmLogin.frm to ASP.NET Core Identity with cookie authentication