using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace ReactChat.Services;

#pragma warning disable OPENAI001

public sealed class WorkflowChatService
{
    private readonly AIProjectClient _projectClient;
    private readonly AgentReference _agentReference;
    private ProjectResponsesClient? _responseClient;
    private const string ApiKeyTokenType = "Bearer";

    public WorkflowChatService(IConfiguration configuration)
    {
        var projectEndpoint = configuration["Workflow:ProjectEndpoint"];
        var agentName = configuration["Workflow:AgentName"];
        var agentVersion = configuration["Workflow:AgentVersion"];
        var apiKey = configuration["Workflow:ApiKey"];

        if (string.IsNullOrWhiteSpace(projectEndpoint))
        {
            throw new InvalidOperationException("Workflow:ProjectEndpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new InvalidOperationException("Workflow:AgentName is not configured.");
        }

        if (string.IsNullOrWhiteSpace(agentVersion))
        {
            throw new InvalidOperationException("Workflow:AgentVersion is not configured.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Workflow:ApiKey is not configured.");
        }

        _projectClient = new AIProjectClient(endpoint: new Uri(projectEndpoint), tokenProvider: new ApiKeyTokenProvider(apiKey));
        _agentReference = new AgentReference(name: agentName, version: agentVersion);
    }

    public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
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

    private sealed class ApiKeyTokenProvider : AuthenticationTokenProvider
    {
        private readonly string _apiKey;

        public ApiKeyTokenProvider(string apiKey)
        {
            _apiKey = apiKey;
        }

        public override GetTokenOptions CreateTokenOptions(IReadOnlyDictionary<string, object> properties)
        {
            return new GetTokenOptions(properties);
        }

        public override AuthenticationToken GetToken(GetTokenOptions options, CancellationToken cancellationToken)
        {
            return new AuthenticationToken(_apiKey, ApiKeyTokenType, DateTimeOffset.MaxValue, refreshOn: null);
        }

        public override ValueTask<AuthenticationToken> GetTokenAsync(GetTokenOptions options, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new AuthenticationToken(_apiKey, ApiKeyTokenType, DateTimeOffset.MaxValue, refreshOn: null));
        }
    }
}
