using Mima.AI.Prompt.Interfaces;

namespace Mima.AI.Prompt.Models;

/// <summary>
/// An ordered list of prompts that form a multi-step workflow (prompt chaining).
/// </summary>
/// <remarks>
/// <para>
/// <b>Pattern:</b> Fluent interface — mutator methods return <c>this</c> (the same instance)
/// so callers can chain calls:
/// <c>Create("name").Add(step1).Add(step2).Add(step3)</c>.
/// </para>
/// <para>
/// <b>What this type is:</b> a named container of <see cref="IPrompt"/> steps in order.
/// It does <b>not</b> call a model or pipe outputs automatically. Your host runs
/// <c>chain[0]</c>, takes the model result, feeds it into <c>chain[1]</c> (e.g. via
/// template variables like <c>{{previousOutput}}</c>), and so on.
/// </para>
/// <para>
/// Typical pipeline: Planner → Writer → Reviewer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var chain = PromptChain.Create("Document Generation")
///     .Add(PromptBuilder.Quick("You are a planner.", "Plan the document structure."))
///     .Add(PromptBuilder.Quick("You are a writer.", "Write based on: {{previousOutput}}"))
///     .Add(PromptBuilder.Quick("You are a reviewer.", "Review and improve: {{previousOutput}}"));
///
/// // Host loop (not in this class):
/// // var outline = await model.Complete(chain[0]);
/// // var draft   = await model.Complete(Render(chain[1], outline));
/// // var final   = await model.Complete(Render(chain[2], draft));
/// </code>
/// </example>
public sealed class PromptChain
{
    private readonly List<IPrompt> _prompts = new();

    /// <summary>Gets the chain name.</summary>
    public string Name { get; }

    /// <summary>Gets the ordered prompts in this chain (step 0 … n-1).</summary>
    public IReadOnlyList<IPrompt> Prompts => _prompts.AsReadOnly();

    /// <summary>Gets the number of steps in this chain.</summary>
    public int StepCount => _prompts.Count;

    private PromptChain(string name)
    {
        Name = name;
    }

    /// <summary>Creates a new empty prompt chain.</summary>
    /// <param name="name">The chain name.</param>
    /// <returns>A new chain instance (not yet containing steps).</returns>
    public static PromptChain Create(string name) => new(name);

    /// <summary>
    /// Appends a prompt as the next step in the workflow.
    /// </summary>
    /// <param name="prompt">The prompt for this step.</param>
    /// <returns>
    /// <c>this</c> — the same <see cref="PromptChain"/> instance — so the next
    /// <see cref="Add"/> can be chained (fluent / builder pattern).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prompt"/> is null.</exception>
    public PromptChain Add(IPrompt prompt)
    {
        _prompts.Add(prompt ?? throw new ArgumentNullException(nameof(prompt)));

        // Fluent pattern: return the current instance, not a copy.
        // Enables: Create("x").Add(a).Add(b).Add(c)
        return this;
    }

    /// <summary>Gets the prompt at the given zero-based step index.</summary>
    /// <param name="step">Zero-based step index.</param>
    public IPrompt this[int step] => _prompts[step];
}
