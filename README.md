# Deployment Assistant

A Blazor WebAssembly sample that uses Microsoft Fluent UI Blazor components. The main page shows a deployment checklist and a chat surface for interacting with a Microsoft Foundation Workflow assistant.

## Getting started

```bash
dotnet run
```

Then open the printed localhost URL.

## Workflow connection

Edit `wwwroot/appsettings.json` (or create `wwwroot/appsettings.Development.json`) with the Azure AI Project endpoint, agent details, and an API key. This client uses the API key directly, so avoid committing real keys to source control.

> Note: This project targets .NET 10. Make sure the .NET 10 SDK is installed before building or running.
