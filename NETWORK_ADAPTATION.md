# Network adaptation guide

This project was scaffolded outside of the secure Azure network as a proof of concept. Use this checklist to adapt the codebase and its dependencies when you move it into the correct network for Microsoft Foundry Agents.

## 1. Package and tooling configuration

- Mirror NuGet dependencies to the approved internal feed and lock restores to it:
  - `dotnet nuget add source <INTERNAL_NUGET_FEED_URL> --name <NAME>`
  - Use a repo-scoped `nuget.config` to enforce the feed and consider enabling `packages.lock.json` for repeatable restores.
- Validate that corporate TLS certificates are trusted by .NET on the build agents (configure the machine trust store as needed).
- Pin the .NET SDK with `global.json` if your environment requires deterministic SDK selection.

## 2. Configuration for service endpoints

Create `wwwroot/appsettings.Development.json` (not committed) to point the UI at in-network services. Suggested keys:

```
{
  "Agents": {
    "ProjectEndpoint": "https://<AI_PROJECT_ENDPOINT>",
    "AgentName": "<DEFAULT_AGENT_NAME>",
    "AgentVersion": "<DEFAULT_AGENT_VERSION>",
    "ApiKey": "<PROJECT_API_KEY>"
  },
  "AppBasePath": "/apps/reactchat"
}
```

- `Agents:ProjectEndpoint`: Azure AI Foundry project endpoint for agents.
- `Agents:AgentName` / `Agents:AgentVersion`: The default agent reference used for general chat.
- `Agents:ApiKey`: Project API key for client-side access (use a key intended for browser use).
- `AppBasePath`: Base path if the app is served behind a reverse proxy or app gateway; use `/` when deployed at the root.

Reference these settings when you wire the UI to real services so that runtime calls resolve to the correct, environment-scoped URLs.

## 3. Blazor and hosting settings

- Update the `<base href>` in `wwwroot/index.html` to match `AppBasePath` if the app is not served from the root path on the internal host.
- Enable HTTPS locally if required by the destination environment's auth policies: use a trusted development certificate or a sanctioned cert.
- When developing inside the secure network, bind the dev server to `0.0.0.0` only if your policy allows LAN access; otherwise leave the default.
- If an internal reverse proxy is present, ensure it forwards the `Accept`, `Authorization`, and `x-ms-*` headers your services require.

## 4. Authentication and authorization

- If the internal Azure AI Foundry endpoint requires Entra ID (Azure AD) instead of an API key, register a single-tenant app and supply the tenant-specific authority.
  - Add placeholders to `wwwroot/appsettings.Development.json` as needed (e.g., `AadTenantId`, `AadClientId`, `AadRedirectUri`).
  - Align redirect URIs with the internal hostname (portal or app gateway address).
- Remove any multi-tenant auth settings used externally; use tenant-restricted scopes and resource URIs inside the secure network.

## 5. Data residency and telemetry

- Point any logging or telemetry SDKs to in-network collectors. If you add Application Insights or Log Analytics, use region-appropriate connection strings and disable browser endpoints that resolve to public hosts.
- Avoid writing PII to client-side storage; align with the destination environment's data handling requirements.

## 6. Build, deployment, and validation

- Build inside the secure network (`dotnet publish -c Release`) to ensure no external fetches occur during bundling.
- Serve the static output from an approved host (e.g., App Service, Storage + Static Web Apps, or an internal static host) that resides on the correct virtual network.
- Validate the bundled asset URLs respect `AppBasePath` and that runtime API calls resolve only to in-network endpoints.
- Run a smoke test with corporate proxies enabled to confirm the UI can reach the Azure AI Foundry endpoints without public egress.

## 7. Hardening checklist before go-live

- Avoid shipping debug symbols in production builds if stack traces should not leak implementation details (use Release builds and trim PDBs as needed).
- Enable Content Security Policy headers that restrict script/style origins to your internal hosts and CDNs.
- Ensure certificates on all target endpoints are trusted by the browsers used inside the network.
- Rotate any keys or webhook secrets used during external testing before moving to the secure environment.

By applying these settings and variables, you can migrate the proof of concept into the secure Azure network while keeping all dependencies and runtime calls confined to approved hosts.
