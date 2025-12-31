# Network adaptation guide

This project was scaffolded outside of the secure Azure network as a proof of concept. Use this checklist to adapt the codebase and its dependencies when you move it into the correct network for Microsoft Foundry Workflow and Power Automate.

## 1. Package and tooling configuration

- Mirror npm dependencies to the approved internal registry and lock installs to it:
  - `npm config set registry <INTERNAL_NPM_REGISTRY>`
  - If offline installation is required, mirror `package-lock.json` and a tarball cache, then install with `npm ci --offline`.
- Validate that corporate TLS certificates are trusted by Node.js in the build agents (set `NODE_EXTRA_CA_CERTS` if the default trust store is insufficient).
- Freeze the toolchain versions already defined in `package.json` to avoid pulling newer artifacts from the public internet.

## 2. Environment variables for service endpoints

Create `.env.local` (not committed) to point the UI at in-network services. Suggested keys:

```
VITE_WORKFLOW_API_BASE=https://<FOUNDATION_WORKFLOW_BASE_URL>
VITE_POWER_AUTOMATE_WEBHOOK=https://<POWER_AUTOMATE_TRIGGER_URL>
VITE_APP_BASE_PATH=/apps/reactchat
```

- `VITE_WORKFLOW_API_BASE`: Base URL for the Foundation Workflow API (in-network host).
- `VITE_POWER_AUTOMATE_WEBHOOK`: HTTP trigger URL for the target Power Automate flow (internal connector or gateway address).
- `VITE_APP_BASE_PATH`: Base path if the app is served behind a reverse proxy or app gateway; use `/` when deployed at the root.

Reference these variables when you wire the UI to real services so that the build emits correct, environment-scoped URLs.

## 3. Vite and hosting settings

- Update `vite.config.ts` to set `base: process.env.VITE_APP_BASE_PATH || '/'` if the app is not served from the root path on the internal host.
- Enable HTTPS locally if required by the destination environment’s auth policies: configure `server.https` with the sanctioned certificate.
- When developing inside the secure network, set `server.host` to `0.0.0.0` only if your policy allows LAN access; otherwise leave the default.
- If an internal reverse proxy is present, ensure it forwards the `Accept`, `Authorization`, and `x-ms-*` headers your services require.

## 4. Authentication and authorization

- If the internal Foundation Workflow endpoint is secured by Entra ID (Azure AD), register a single-tenant app and supply the tenant-specific authority.
  - Add placeholders to `.env.local` as needed (e.g., `VITE_AAD_TENANT_ID`, `VITE_AAD_CLIENT_ID`, `VITE_AAD_REDIRECT_URI`).
  - Align redirect URIs with the internal hostname (portal or app gateway address).
- Remove any multi-tenant auth settings used externally; use tenant-restricted scopes and resource URIs inside the secure network.

## 5. Power Automate connectivity

- If using HTTP triggers, ensure the internal firewall allows outbound calls from the hosting environment to the flow’s endpoint.
- Prefer an on-premises data gateway or managed connector over public HTTP URLs when available; update `VITE_POWER_AUTOMATE_WEBHOOK` accordingly.
- Confirm the flow’s CORS and authentication settings accept requests from the internal web host.

## 6. Data residency and telemetry

- Point any logging or telemetry SDKs to in-network collectors. If you add Application Insights or Log Analytics, use region-appropriate connection strings and disable browser endpoints that resolve to public hosts.
- Avoid writing PII to client-side storage; align with the destination environment’s data handling requirements.

## 7. Build, deployment, and validation

- Build inside the secure network (`npm run build`) to ensure no external fetches occur during bundling.
- Serve the static output from an approved host (e.g., App Service, Storage + Static Web Apps, or an internal static host) that resides on the correct virtual network.
- Validate the bundled asset URLs respect `VITE_APP_BASE_PATH` and that runtime API calls resolve only to in-network endpoints.
- Run a smoke test with corporate proxies enabled to confirm the UI can reach the Foundation Workflow and Power Automate URLs without public egress.

## 8. Hardening checklist before go-live

- Disable source maps in production builds if stack traces should not leak implementation details (`build.sourcemap = false` in `vite.config.ts`).
- Enable Content Security Policy headers that restrict script/style origins to your internal hosts and CDNs.
- Ensure certificates on all target endpoints are trusted by the browsers used inside the network.
- Rotate any keys or webhook secrets used during external testing before moving to the secure environment.

By applying these settings and variables, you can migrate the proof of concept into the secure Azure network while keeping all dependencies and runtime calls confined to approved hosts.
