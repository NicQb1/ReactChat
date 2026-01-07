using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using OpenAI.Responses;

namespace ReactChat.Services;

#pragma warning disable OPENAI001

public sealed class WorkflowChatService
{
    private readonly AIProjectClient _projectClient;
    private readonly AgentReference _agentReference;
    private ProjectResponsesClient? _responseClient;

    public WorkflowChatService(IConfiguration configuration)
    {
        var projectEndpoint = configuration["Workflow:ProjectEndpoint"];
        var agentName = configuration["Workflow:AgentName"];
        var agentVersion = configuration["Workflow:AgentVersion"];

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

        _projectClient = new AIProjectClient(endpoint: new Uri(projectEndpoint), tokenProvider: new DefaultAzureCredential());
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
}
