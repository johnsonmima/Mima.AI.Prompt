using SaaFarr.AI.Prompt.Templates;

namespace SaaFarr.AI.Prompt.Catalog;

/// <summary>
/// Built-in user message templates for common request patterns.
/// These templates represent repeatable user intents that can be parameterized.
/// </summary>
/// <example>
/// <code>
/// var prompt = PromptBuilder
///     .Use(SystemTemplates.Teacher)
///     .AddTemplate(UserTemplates.ExplainConcept)
///     .With("topic", "Dependency Injection")
///     .Build();
/// </code>
/// </example>
public static class UserTemplates
{
    /// <summary>Summarize a document.</summary>
    public static UserTemplate Summarize { get; } = UserTemplate.Create(
        "Summarize",
        """
        Summarize the following content in {{style}} style:

        {{content}}
        """);

    /// <summary>Translate text to a target language.</summary>
    public static UserTemplate Translate { get; } = UserTemplate.Create(
        "Translate",
        """
        Translate the following text to {{targetLanguage}}:

        {{text}}
        """);

    /// <summary>Extract entities from text.</summary>
    public static UserTemplate ExtractEntities { get; } = UserTemplate.Create(
        "Extract Entities",
        """
        Extract all {{entityType}} entities from the following text. Return them as a JSON array.

        {{text}}
        """);

    /// <summary>Review a resume.</summary>
    public static UserTemplate ReviewResume { get; } = UserTemplate.Create(
        "Review Resume",
        """
        Review the following resume for a {{role}} position.
        Provide: Strengths, Weaknesses, Suggestions for improvement.

        {{resume}}
        """);

    /// <summary>Generate SQL from a natural language question.</summary>
    public static UserTemplate GenerateSql { get; } = UserTemplate.Create(
        "Generate SQL",
        """
        Given the following table schema:
        {{schema}}

        Write a SQL query to: {{question}}
        """);

    /// <summary>Generate documentation for code.</summary>
    public static UserTemplate GenerateDocumentation { get; } = UserTemplate.Create(
        "Generate Documentation",
        """
        Generate {{documentationType}} documentation for the following code:

        ```{{language}}
        {{code}}
        ```
        """);

    /// <summary>Create unit tests for code.</summary>
    public static UserTemplate GenerateTests { get; } = UserTemplate.Create(
        "Generate Tests",
        """
        Write unit tests for the following {{language}} code using {{framework}}:

        ```{{language}}
        {{code}}
        ```

        Cover: happy path, edge cases, and error scenarios.
        """);

    /// <summary>Explain a concept clearly.</summary>
    public static UserTemplate ExplainConcept { get; } = UserTemplate.Create(
        "Explain Concept",
        """
        Explain {{topic}} in simple terms.
        Use analogies and examples.
        Target audience: {{audience}}.
        """);

    /// <summary>Review code for quality issues.</summary>
    public static UserTemplate ReviewCode { get; } = UserTemplate.Create(
        "Review Code",
        """
        Review the following {{language}} code for:
        - Performance issues
        - Security vulnerabilities
        - Maintainability concerns
        - Best practice violations

        ```{{language}}
        {{code}}
        ```
        """);

    /// <summary>Convert data to JSON format.</summary>
    public static UserTemplate ConvertToJson { get; } = UserTemplate.Create(
        "Convert to JSON",
        """
        Convert the following data into a well-structured JSON format:

        {{data}}
        """);

    /// <summary>Generate a professional email.</summary>
    public static UserTemplate GenerateEmail { get; } = UserTemplate.Create(
        "Generate Email",
        """
        Write a professional email.
        Audience: {{audience}}
        Tone: {{tone}}
        Purpose: {{purpose}}
        Key points to include: {{keyPoints}}
        """);

    /// <summary>RAG (Retrieval Augmented Generation) pattern.</summary>
    public static UserTemplate Rag { get; } = UserTemplate.Create(
        "RAG",
        """
        Context:
        {{documents}}

        Question: {{question}}

        Answer the question using only the provided context. If the context does not contain enough information, say so.
        """);

    /// <summary>Chain-of-thought reasoning prompt.</summary>
    public static UserTemplate ChainOfThought { get; } = UserTemplate.Create(
        "Chain of Thought",
        """
        {{question}}

        Think step by step:
        1. Identify the key components of the problem.
        2. Break down the solution into logical steps.
        3. Show your work at each step.
        4. Arrive at the final answer.
        """);

    /// <summary>Compare two options or approaches.</summary>
    public static UserTemplate Compare { get; } = UserTemplate.Create(
        "Compare",
        """
        Compare {{optionA}} vs {{optionB}} for {{useCase}}.

        Consider: pros, cons, performance, ease of use, and when to choose each.
        Present as a structured comparison.
        """);

    /// <summary>Generate API documentation from code.</summary>
    public static UserTemplate GenerateApiDocs { get; } = UserTemplate.Create(
        "Generate API Docs",
        """
        Generate REST API documentation for the following endpoint:

        {{endpoint}}

        Include: HTTP method, URL, request/response body, status codes, headers, and example usage.
        """);

    /// <summary>Create a user story from a feature description.</summary>
    public static UserTemplate CreateUserStory { get; } = UserTemplate.Create(
        "Create User Story",
        """
        Create a user story for the following feature:

        {{feature}}

        Format:
        As a {{persona}}, I want to [action] so that [benefit].

        Include acceptance criteria as a checklist.
        """);

    /// <summary>Analyze sentiment of text.</summary>
    public static UserTemplate AnalyzeSentiment { get; } = UserTemplate.Create(
        "Analyze Sentiment",
        """
        Analyze the sentiment of the following text:

        {{text}}

        Provide: overall sentiment (positive/negative/neutral), confidence score, and key phrases that influenced the rating.
        """);

    /// <summary>Generate a commit message from a code diff.</summary>
    public static UserTemplate GenerateCommitMessage { get; } = UserTemplate.Create(
        "Generate Commit Message",
        """
        Generate a conventional commit message for the following diff:

        {{diff}}

        Follow conventional commits format: type(scope): description
        Include a brief body if the change is complex.
        """);

    /// <summary>Refactor code for better quality.</summary>
    public static UserTemplate RefactorCode { get; } = UserTemplate.Create(
        "Refactor Code",
        """
        Refactor the following {{language}} code to improve {{goal}}:

        ```{{language}}
        {{code}}
        ```

        Explain what you changed and why. Preserve existing behavior.
        """);

    /// <summary>Create a project plan from requirements.</summary>
    public static UserTemplate CreateProjectPlan { get; } = UserTemplate.Create(
        "Create Project Plan",
        """
        Create a project plan for:

        {{requirements}}

        Include: phases, tasks, estimated duration, dependencies, and milestones.
        Target timeline: {{timeline}}.
        """);

    /// <summary>Write a technical blog post.</summary>
    public static UserTemplate WriteBlogPost { get; } = UserTemplate.Create(
        "Write Blog Post",
        """
        Write a technical blog post about {{topic}}.

        Target audience: {{audience}}
        Tone: {{tone}}
        Length: approximately {{wordCount}} words.

        Include: introduction, key sections, code examples where relevant, and a conclusion.
        """);

    /// <summary>Generate test cases from requirements.</summary>
    public static UserTemplate GenerateTestCases { get; } = UserTemplate.Create(
        "Generate Test Cases",
        """
        Generate test cases for the following requirement:

        {{requirement}}

        Include: test name, preconditions, steps, expected result, and priority (Critical/High/Medium/Low).
        Cover: happy path, edge cases, negative scenarios, and boundary conditions.
        """);

    /// <summary>Explain an error and suggest a fix.</summary>
    public static UserTemplate ExplainError { get; } = UserTemplate.Create(
        "Explain Error",
        """
        Explain the following error and suggest how to fix it:

        Error: {{error}}

        Context:
        ```{{language}}
        {{code}}
        ```

        Explain: what caused it, why it happens, and how to prevent it in the future.
        """);

    /// <summary>Create a data model or schema from requirements.</summary>
    public static UserTemplate DesignSchema { get; } = UserTemplate.Create(
        "Design Schema",
        """
        Design a {{schemaType}} schema for:

        {{requirements}}

        Consider: relationships, constraints, indexing strategy, and normalization level.
        Provide the schema definition and explain design decisions.
        """);

    /// <summary>Generate a regex pattern from natural language.</summary>
    public static UserTemplate GenerateRegex { get; } = UserTemplate.Create(
        "Generate Regex",
        """
        Create a regex pattern that matches: {{description}}

        Provide:
        - The regex pattern
        - Explanation of each part
        - Example matches
        - Example non-matches
        """);

    /// <summary>Create a meeting agenda from topics.</summary>
    public static UserTemplate CreateAgenda { get; } = UserTemplate.Create(
        "Create Agenda",
        """
        Create a meeting agenda for:

        Meeting: {{meetingName}}
        Duration: {{duration}}
        Attendees: {{attendees}}
        Topics: {{topics}}

        Include time allocations, discussion points, and desired outcomes for each item.
        """);

    /// <summary>Generate a code review checklist.</summary>
    public static UserTemplate CodeReviewChecklist { get; } = UserTemplate.Create(
        "Code Review Checklist",
        """
        Generate a code review checklist for a {{language}} {{projectType}} project.

        Focus areas: {{focusAreas}}

        Include categories: correctness, security, performance, maintainability, testing, documentation.
        """);

    /// <summary>Self-evaluation prompt - ask the model to critique its own output.</summary>
    public static UserTemplate SelfEvaluate { get; } = UserTemplate.Create(
        "Self Evaluate",
        """
        Evaluate the following response for quality:

        {{response}}

        Score on a scale of 1-10 for:
        - Accuracy
        - Completeness
        - Clarity
        - Usefulness

        Provide specific suggestions for improvement.
        """);

    /// <summary>Convert unstructured text to structured data.</summary>
    public static UserTemplate ExtractStructuredData { get; } = UserTemplate.Create(
        "Extract Structured Data",
        """
        Extract structured data from the following text:

        {{text}}

        Output format: {{format}}

        Extract: {{fields}}
        """);
}
