I have a VB6 MDI application called Salmon King Seafood (SKS). The source code is at
/Users/kilzdavi/Projects/aws-transform/SKSVB6/. It has 15+ forms including order reception, customer management,product catalog, stock management, and approval workflows. It uses 3 modules (modConnection.bas for database connectivity, modFunctions.bas for utility functions, modMain.bas for application entry point). The database is SQLite accessed through ADO data controls with data binding. Controls include MSFlexGrid, ListView, Toolbar, ImageList, and Control Arrays. Transform this to an ASP.NET Core Blazor Server application targeting .NET 10.

I have reference migration paters on how to vb6 should convert from VB6 to c# located at '/Users/kilzdavi/Projects/aws-transform/vb6-csharp-references/migration-paterns.md', references to our organizations coding standards and business rules at '/Users/kilzdavi/Projects/aws-transform/vb6-csharp-references/coding-standards.md', and target architecture pattern located at '/Users/kilzdavi/Projects/aws-transform/vb6-csharp-references/target-architecture.md'.

I have reference documentation for this transformation:
- Example transformations: '/Users/kilzdavi/Projects/aws-transform/vb6-csharp-references/migration-paterns.md'
- Coding standards: '/Users/kilzdavi/Projects/aws-transform/vb6-csharp-references/coding-standards.md'
- Target architecture: '/Users/kilzdavi/Projects/aws-transform/vb6-csharp-references/target-architecture.md'

atx custom def exec -p /mnt/c/Projects/SKSVB6 \
   -n 'AWS/early-access-comprehensive-codebase-analysis' \
   -c 'dotnet build' \
   --configuration 'additionalPlanContext=Target .NET 8 with C# 12 features, use Blazor Server, apply company coding standards. Convert MDI forms to Blazor page components. Convert modConnection.bas to a DbContext service. Convert modFunctions.bas to extension method classes. Convert ADO data controls to Entity Framework Core with SQLite provider.' \
   -x -t 

atx custom def exec \
    --code-repository-path '/Users/kilzdavi/Projects/aws-transform/SKSVB6' \
    --non-interactive \
    --trust-all-tools \
    --campaign 2b60b64d-5b6a-4d06-90fa-c112b04e5c42 \
    --configuration "additionalPlanContext=Target .NET 10 with C# 14 features, use Blazor Server, apply company coding standards. Convert MDI forms to Blazor page components. Convert modConnection.bas to a DbContext service. Convert modFunctions.bas to extension method classes. Convert ADO data controls to Entity Framework Core with SQLite provider." \
    --repo-name SKSVB6 \
    --add-repo

    