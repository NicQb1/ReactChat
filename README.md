# Deployment Assistant

A Blazor WebAssembly sample that uses Microsoft Fluent UI Blazor components. The main page shows a deployment checklist and a chat surface for interacting with Microsoft Foundry Agents.

## Getting started

```bash
dotnet run
```

Then open the printed localhost URL.

## Agents connection

Edit `wwwroot/appsettings.json` (or create `wwwroot/appsettings.Development.json`) with the Azure AI Project endpoint, agent details, and MSAL settings. Configure:

- `AzureAd:Authority` and `AzureAd:ClientId` for the Entra ID app registration.
- `Agents:Scopes` with the Azure AI Foundry scope (for example `https://ai.azure.com/.default`).
- Optional task-to-agent mappings under `Agents:TaskAgents` (for example `gather-info` and `create-steps`).

Sign in at `/authentication/login` before sending messages if prompted.

> Note: This project targets .NET 10. Make sure the .NET 10 SDK is installed before building or running.
