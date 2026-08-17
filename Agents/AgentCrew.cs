using System.Text;
using SaaFarr.AI.Prompt.Builder;
using SaaFarr.AI.Prompt.Content;
using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Messages;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Agents;

/// <summary>
/// Thin multi-agent crew sugar: custom roles + optional speaker names composed into one prompt turn.
/// Does not schedule speakers or run debates — the host decides turn order and model calls.
/// </summary>
public sealed class AgentCrew
{
    private readonly List<AgentMember> _members = new();
    private string? _orchestratorInstructions;
    private OutputFormat? _responseFormat;

    /// <summary>Gets the crew name.</summary>
    public string Name { get; }

    /// <summary>Gets crew members in add order.</summary>
    public IReadOnlyList<AgentMember> Members => _members.AsReadOnly();

    private AgentCrew(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Crew name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    /// <summary>Creates a new crew.</summary>
    public static AgentCrew Create(string name) => new(name);

    /// <summary>Sets orchestrator / host system instructions for the crew turn.</summary>
    public AgentCrew WithOrchestratorInstructions(string instructions)
    {
        _orchestratorInstructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
        return this;
    }

    /// <summary>Adds a crew member (custom role).</summary>
    /// <param name="roleName">Custom role name (e.g. <c>writer</c>, <c>critic</c>).</param>
    /// <param name="instructions">Member instructions for this turn.</param>
    /// <param name="displayName">Optional <see cref="IMessage.Name"/> speaker id.</param>
    public AgentCrew AddMember(string roleName, string instructions, string? displayName = null)
    {
        _members.Add(new AgentMember(roleName, instructions, displayName));
        return this;
    }

    /// <summary>Adds a pre-built member.</summary>
    public AgentCrew AddMember(AgentMember member)
    {
        _members.Add(member ?? throw new ArgumentNullException(nameof(member)));
        return this;
    }

    /// <summary>Optional structured response format for the crew prompt.</summary>
    public AgentCrew WithResponseFormat(OutputFormat format)
    {
        _responseFormat = format ?? throw new ArgumentNullException(nameof(format));
        return this;
    }

    /// <summary>
    /// Builds a prompt for a topic: orchestrator system message, each member as a custom-role turn,
    /// then a user topic message.
    /// </summary>
    public Models.Prompt BuildTurn(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be empty.", nameof(topic));
        if (_members.Count == 0)
            throw new InvalidOperationException("Add at least one crew member before BuildTurn.");

        var builder = PromptBuilder.Create()
            .AddSystem(ComposeOrchestratorSystem())
            .WithName(Name);

        foreach (var member in _members)
        {
            var role = MessageRole.Custom(member.RoleName);
            var message = CustomMessage.Create(
                role,
                parts: new IContentPart[] { TextPart.Create(member.Instructions) },
                name: member.DisplayName);
            builder.AddMessage(message);
        }

        builder.AddUser(topic);

        if (_responseFormat is not null)
            builder.WithResponseFormat(_responseFormat);

        return builder.Build();
    }

    /// <summary>
    /// Builds a prompt from explicit member contributions (role name → content), plus a user topic.
    /// </summary>
    public Models.Prompt BuildTurn(string topic, IEnumerable<(string RoleName, string Content)> contributions)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be empty.", nameof(topic));
        if (contributions is null)
            throw new ArgumentNullException(nameof(contributions));

        var builder = PromptBuilder.Create()
            .AddSystem(ComposeOrchestratorSystem())
            .WithName(Name);

        foreach (var (roleName, content) in contributions)
        {
            var member = _members.FirstOrDefault(m =>
                string.Equals(m.RoleName, roleName, StringComparison.OrdinalIgnoreCase));
            var role = MessageRole.Custom(roleName);
            builder.AddMessage(CustomMessage.Create(
                role,
                parts: new IContentPart[] { TextPart.Create(content) },
                name: member?.DisplayName));
        }

        builder.AddUser(topic);

        if (_responseFormat is not null)
            builder.WithResponseFormat(_responseFormat);

        return builder.Build();
    }

    private string ComposeOrchestratorSystem()
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(_orchestratorInstructions))
            sb.Append(_orchestratorInstructions!.Trim());
        else
            sb.Append("You orchestrate a multi-agent crew. Keep roles distinct and turns focused.");

        if (_members.Count > 0)
        {
            sb.Append("\n\nCrew members:");
            foreach (var member in _members)
            {
                sb.Append("\n- ").Append(member.RoleName);
                if (member.DisplayName is not null)
                    sb.Append(" (").Append(member.DisplayName).Append(')');
            }
        }

        return sb.ToString();
    }
}

/// <summary>A named crew member with instructions and optional speaker display name.</summary>
public sealed class AgentMember
{
    /// <summary>Custom role name.</summary>
    public string RoleName { get; }

    /// <summary>Instructions / opening line for this member.</summary>
    public string Instructions { get; }

    /// <summary>Optional message <c>name</c> (multi-agent speaker id).</summary>
    public string? DisplayName { get; }

    /// <summary>Creates a crew member.</summary>
    public AgentMember(string roleName, string instructions, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ArgumentException("Role name cannot be empty.", nameof(roleName));
        if (string.IsNullOrWhiteSpace(instructions))
            throw new ArgumentException("Instructions cannot be empty.", nameof(instructions));

        RoleName = roleName.Trim();
        Instructions = instructions.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName!.Trim();
    }
}
