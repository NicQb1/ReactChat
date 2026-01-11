# Deployment Assistant

A Blazor WebAssembly sample that uses Microsoft Fluent UI Blazor components. The main page shows a deployment checklist and a chat surface for interacting with Microsoft Foundry Agents.

## Getting started

```bash
dotnet run
```

Then open the printed localhost URL.

## Agents connection

Edit `wwwroot/appsettings.json` (or create `wwwroot/appsettings.Development.json`) with the Azure AI Project endpoint, agent details, and the project API key (`Agents:ApiKey`). The Blazor WebAssembly client sends the API key with each request, so use a key intended for client-side use. You can optionally map task IDs to persistent agent IDs under `Agents:TaskAgents` (for example `gather-info` and `create-steps`).

> Note: This project targets .NET 10. Make sure the .NET 10 SDK is installed before building or running.
