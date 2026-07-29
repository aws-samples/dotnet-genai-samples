# Transformation Plan: RiskCalculatorWebForm

- **Solution Path**: d8aec91d-0ea3-4260-9766-b1df34e3cf33_extracted/sourceCode/RiskCalculatorWebForm.sln
- **Created**: 2026-05-15T22:52:48Z
- **Default Target Framework**: net10.0

## Projects

### 1. risk-calculator-webapp

- **Path**: d8aec91d-0ea3-4260-9766-b1df34e3cf33_extracted/sourceCode/risk-calculator-webapp/risk-calculator-webapp.csproj
- **Current Framework**: v4.8
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of risk-calculator-webapp from v4.8. 6 incompatible NuGet package(s) require replacement. 14 NuGet reference(s) to review.
- **Estimated Changes**: 32
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.FriendlyUrls
  - Incompatible NuGet package: Microsoft.AspNet.FriendlyUrls.Core
  - Incompatible NuGet package: Microsoft.AspNet.ScriptManager.MSAjax
  - Incompatible NuGet package: Microsoft.AspNet.ScriptManager.WebForms
  - Incompatible NuGet package: Microsoft.AspNet.Web.Optimization
  - Incompatible NuGet package: Microsoft.AspNet.Web.Optimization.WebForms
- **Notes**: Moderate NuGet dependency count (14)

#### Dependencies

- None

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| bootstrap | 5.2.3 | 5.3.0 | yes |
| jQuery | 3.7.0 | 3.7.0 | yes |
| Microsoft.AspNet.FriendlyUrls | 1.0.2 | 1.0.2 | yes |
| Microsoft.AspNet.ScriptManager.MSAjax | 5.0.0 | 5.0.0 | yes |
| Microsoft.AspNet.ScriptManager.WebForms | 5.0.0 | 5.0.0 | yes |
| Modernizr | 2.8.3 | 2.8.3 | yes |
| Newtonsoft.Json | 13.0.3 | 13.0.4 | yes |
| System.ValueTuple | 4.6.1 | 4.6.2 | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.FriendlyUrls
- Incompatible NuGet package: Microsoft.AspNet.FriendlyUrls.Core
- Incompatible NuGet package: Microsoft.AspNet.ScriptManager.MSAjax
- Incompatible NuGet package: Microsoft.AspNet.ScriptManager.WebForms
- Incompatible NuGet package: Microsoft.AspNet.Web.Optimization
- Incompatible NuGet package: Microsoft.AspNet.Web.Optimization.WebForms
