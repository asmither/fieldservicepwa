# Field Service PWA

Legacy ICS mobile field service app published as a focused Blazor WebAssembly repository.

## Projects

- `ICS.Mobile` - mobile Blazor WebAssembly PWA.
- `ICS.DesignSystem` - shared Razor class library for design tokens and reusable UI primitives.
- `ICS.Portal.Auth.Models` - authentication DTOs referenced by the mobile app.

## Build

Open `FieldServicePwa.sln` in Visual Studio, or build the app project directly:

```sh
dotnet build ICS.Mobile/ICS.Mobile.csproj
```

Local Azure Functions settings are intentionally not included in this public repo.
