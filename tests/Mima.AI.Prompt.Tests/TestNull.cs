namespace Mima.AI.Prompt.Tests;

/// <summary>
/// Null arguments for APIs that throw, without the null-forgiving operator at call sites.
/// </summary>
internal static class TestNull
{
    public static T Ref<T>() where T : class
    {
#pragma warning disable CS8603
        return null;
#pragma warning restore CS8603
    }
}

/// <summary>Fails the test if a nullable value is missing instead of force-unwrapping.</summary>
internal static class Must
{
    public static T Be<T>(T? value) where T : class =>
        value ?? throw new InvalidOperationException($"Expected a non-null {typeof(T).Name}.");
}
