# Deployment Assistant

A Blazor WebAssembly sample that uses Microsoft Fluent UI Blazor components. The main page shows a deployment checklist and a chat surface for interacting with a Microsoft Foundation Workflow assistant.

## Getting started

```bash
dotnet run
```

Then open the printed localhost URL.

## Workflow connection

Edit `wwwroot/appsettings.json` with the Azure AI Project endpoint and agent details. Authentication uses `DefaultAzureCredential`, so your local environment must be signed in (for example via `az login`) or running under a managed identity.

> Note: This project targets .NET 10. Make sure the .NET 10 SDK is installed before building or running.
