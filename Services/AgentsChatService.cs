using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using PersistentMessageRole = Azure.AI.Agents.Persistent.MessageRole;

namespace ReactChat.Services;

#pragma warning disable OPENAI001

public sealed class AgentsChatService
{
    private readonly AIProjectClient _projectClient;
    private readonly AgentReference _agentReference;
    private readonly PersistentAgentsClient _persistentAgentsClient;
    private readonly Dictionary<string, PersistentAgentState> _persistentAgentStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyDictionary<string, string> _taskAgentIds;
    private ProjectResponsesClient? _responseClient;

    public AgentsChatService(
        IConfiguration configuration,
        IAccessTokenProvider accessTokenProvider,
        NavigationManager navigationManager)
    {
        var projectEndpoint = configuration["Agents:ProjectEndpoint"];
        var agentName = configuration["Agents:AgentName"];
        var agentVersion = configuration["Agents:AgentVersion"];
        var scopes = configuration.GetSection("Agents:Scopes").Get<string[]>();
        var gatherInfoAgentId = configuration["Agents:TaskAgents:gather-info"];
        var createStepsAgentId = configuration["Agents:TaskAgents:create-steps"];

        if (string.IsNullOrWhiteSpace(projectEndpoint))
        {
            throw new InvalidOperationException("Agents:ProjectEndpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new InvalidOperationException("Agents:AgentName is not configured.");
        }

        if (string.IsNullOrWhiteSpace(agentVersion))
        {
            throw new InvalidOperationException("Agents:AgentVersion is not configured.");
        }

        if (scopes is null || scopes.Length == 0)
        {
            throw new InvalidOperationException("Agents:Scopes is not configured.");
        }

        var tokenSource = new MsalTokenSource(accessTokenProvider, navigationManager, scopes);
        _projectClient = new AIProjectClient(endpoint: new Uri(projectEndpoint), tokenProvider: new MsalAuthenticationTokenProvider(tokenSource));
        _persistentAgentsClient = new PersistentAgentsClient(projectEndpoint, new MsalTokenCredential(tokenSource));
        _agentReference = new AgentReference(name: agentName, version: agentVersion);
        _taskAgentIds = BuildTaskAgentMap(gatherInfoAgentId, createStepsAgentId);
    }

    public async Task<string> SendMessageAsync(string message, string? taskId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(taskId) && _taskAgentIds.TryGetValue(taskId, out var agentId))
        {
            return await SendPersistentAgentMessageAsync(agentId, message, cancellationToken);
        }

        var responseClient = await GetResponseClientAsync(cancellationToken);
        var response = await responseClient.CreateResponseAsync(message, cancellationToken: cancellationToken);

        return response.Value.GetOutputText();
    }

    private async Task<ProjectResponsesClient> GetResponseClientAsync(CancellationToken cancellationToken)
    {
        if (_responseClient is not null)
        {
            return _responseClient;
        }

        ProjectConversation conversation = await _projectClient.OpenAI.Conversations.CreateProjectConversationAsync(cancellationToken: cancellationToken);
        _responseClient = _projectClient.OpenAI.GetProjectResponsesClientForAgent(_agentReference, conversation);

        return _responseClient;
    }

    private static IReadOnlyDictionary<string, string> BuildTaskAgentMap(string? gatherInfoAgentId, string? createStepsAgentId)
    {
        var taskAgents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(gatherInfoAgentId))
        {
            taskAgents["gather-info"] = gatherInfoAgentId;
        }

        if (!string.IsNullOrWhiteSpace(createStepsAgentId))
        {
            taskAgents["create-steps"] = createStepsAgentId;
        }

        return taskAgents;
    }

    private async Task<string> SendPersistentAgentMessageAsync(string agentId, string message, CancellationToken cancellationToken)
    {
        var state = await GetPersistentAgentStateAsync(agentId, cancellationToken);

        await _persistentAgentsClient.Messages.CreateMessageAsync(
            state.Thread.Id,
            PersistentMessageRole.User,
            message,
            attachments: null,
            metadata: null,
            cancellationToken: cancellationToken);

        var runResponse = await _persistentAgentsClient.Runs.CreateRunAsync(state.Thread, state.Agent, cancellationToken);
        var run = await WaitForRunAsync(state.Thread.Id, runResponse.Value.Id, cancellationToken);

        if (run.Status != RunStatus.Completed)
        {
            throw new InvalidOperationException($"Run failed or was canceled: {run.LastError?.Message ?? run.Status.ToString()}");
        }

        return await GetLatestAssistantMessageAsync(state.Thread.Id, run.Id, cancellationToken);
    }

    private async Task<PersistentAgentState> GetPersistentAgentStateAsync(string agentId, CancellationToken cancellationToken)
    {
        if (_persistentAgentStates.TryGetValue(agentId, out var state))
        {
            return state;
        }

        var agentResponse = await ResolveAgentAsync(agentId, cancellationToken);
        var threadResponse = await _persistentAgentsClient.Threads.CreateThreadAsync(
            messages: Array.Empty<ThreadMessageOptions>(),
            toolResources: null,
            metadata: null,
            cancellationToken: cancellationToken);

        state = new PersistentAgentState(agentResponse, threadResponse.Value);
        _persistentAgentStates[agentId] = state;

        return state;
    }

    private async Task<PersistentAgent> ResolveAgentAsync(string agentIdentifier, CancellationToken cancellationToken)
    {
        if (agentIdentifier.StartsWith("asst_", StringComparison.OrdinalIgnoreCase))
        {
            var response = await _persistentAgentsClient.Administration.GetAgentAsync(agentIdentifier, cancellationToken);
            return response.Value;
        }

        await foreach (var agent in _persistentAgentsClient.Administration.GetAgentsAsync(
            limit: null,
            order: null,
            after: null,
            before: null,
            cancellationToken: cancellationToken))
        {
            if (string.Equals(agent.Name, agentIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                return agent;
            }
        }

        throw new InvalidOperationException($"Agent '{agentIdentifier}' was not found.");
    }

    private async Task<ThreadRun> WaitForRunAsync(string threadId, string runId, CancellationToken cancellationToken)
    {
        var runResponse = await _persistentAgentsClient.Runs.GetRunAsync(threadId, runId, cancellationToken);
        ThreadRun run = runResponse.Value;
        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            runResponse = await _persistentAgentsClient.Runs.GetRunAsync(threadId, runId, cancellationToken);
            run = runResponse.Value;
        }

        return run;
    }

    private async Task<string> GetLatestAssistantMessageAsync(string threadId, string runId, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();

        await foreach (var threadMessage in _persistentAgentsClient.Messages.GetMessagesAsync(
            threadId,
            before: null,
            limit: null,
            order: ListSortOrder.Ascending,
            after: null,
            runId: runId,
            cancellationToken: cancellationToken))
        {
            if (threadMessage.Role != PersistentMessageRole.Agent)
            {
                continue;
            }

            builder.Clear();
            foreach (var contentItem in threadMessage.ContentItems)
            {
                if (contentItem is MessageTextContent textItem)
                {
                    builder.Append(textItem.Text);
                }
            }
        }

        return builder.ToString();
    }

    private sealed class MsalTokenSource
    {
        private readonly IAccessTokenProvider _accessTokenProvider;
        private readonly NavigationManager _navigationManager;
        private readonly string[] _scopes;

        public MsalTokenSource(
            IAccessTokenProvider accessTokenProvider,
            NavigationManager navigationManager,
            string[] scopes)
        {
            _accessTokenProvider = accessTokenProvider;
            _navigationManager = navigationManager;
            _scopes = scopes;
        }

        public async ValueTask<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync()
        {
            var result = await _accessTokenProvider.RequestAccessToken(new AccessTokenRequestOptions
            {
                Scopes = _scopes,
                ReturnUrl = _navigationManager.Uri,
            });

            if (result.TryGetToken(out var token))
            {
                return (token.Value, token.Expires);
            }

            if (result.Status == AccessTokenResultStatus.RequiresRedirect)
            {
                if (!string.IsNullOrWhiteSpace(result.InteractiveRequestUrl))
                {
                    _navigationManager.NavigateTo(result.InteractiveRequestUrl);
                    throw new InvalidOperationException("Redirecting to sign-in.");
                }

                throw new InvalidOperationException("Sign-in is required. Navigate to /authentication/login.");
            }

            throw new InvalidOperationException($"Failed to acquire an access token. Status: {result.Status}.");
        }
    }

    private sealed class MsalAuthenticationTokenProvider : AuthenticationTokenProvider
    {
        private const string TokenType = "Bearer";
        private readonly MsalTokenSource _tokenSource;

        public MsalAuthenticationTokenProvider(MsalTokenSource tokenSource)
        {
            _tokenSource = tokenSource;
        }

        public override GetTokenOptions CreateTokenOptions(IReadOnlyDictionary<string, object> properties)
        {
            return new GetTokenOptions(properties);
        }

        public override AuthenticationToken GetToken(GetTokenOptions options, CancellationToken cancellationToken)
        {
            return GetTokenAsync(options, cancellationToken).GetAwaiter().GetResult();
        }

        public override async ValueTask<AuthenticationToken> GetTokenAsync(GetTokenOptions options, CancellationToken cancellationToken)
        {
            var (token, expiresOn) = await _tokenSource.GetTokenAsync();
            return new AuthenticationToken(token, TokenType, expiresOn, refreshOn: null);
        }
    }

    private sealed class MsalTokenCredential : TokenCredential
    {
        private readonly MsalTokenSource _tokenSource;

        public MsalTokenCredential(MsalTokenSource tokenSource)
        {
            _tokenSource = tokenSource;
        }

        public override Azure.Core.AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();
        }

        public override async ValueTask<Azure.Core.AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var (token, expiresOn) = await _tokenSource.GetTokenAsync();
            return new Azure.Core.AccessToken(token, expiresOn);
        }
    }

    private sealed record PersistentAgentState(PersistentAgent Agent, PersistentAgentThread Thread);
}
