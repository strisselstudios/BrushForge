# BrushForge

BrushForge is a Windows desktop application for generating valid convex brush geometry and exporting editable TrenchBroom-compatible Valve 220 `.map` files.

## Repository structure

- `src/` contains the BrushForge application and supporting libraries.
- `tests/BrushForge.Tests/` contains the automated test suite.
- `docs/` contains technical documentation for implemented systems.
- `BrushForge.sln` is the main Visual Studio solution.

## Build

From the repository root:

```powershell
dotnet build .\BrushForge.sln --configuration Debug
```

## Test

```powershell
dotnet test .\tests\BrushForge.Tests\BrushForge.Tests.csproj --configuration Debug
```
