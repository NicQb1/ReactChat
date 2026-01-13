using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using ReactChat;
using ReactChat.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddFluentUIComponents();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

    var scopes = builder.Configuration.GetSection("Agents:Scopes").Get<string[]>();
    if (scopes is { Length: > 0 })
    {
        foreach (var scope in scopes)
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
        }
    }
});
builder.Services.AddScoped<AgentsChatService>();

await builder.Build().RunAsync();
