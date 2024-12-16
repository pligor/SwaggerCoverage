namespace SwaggerCoverage;

/// <summary>
/// Represents a pairing of an invocation location with its corresponding method definition.
/// </summary>
public class InvocationDefinitionPair
{
    public required InvocationLocation Invocation { get; set; }
    public required MethodInfo Definition { get; set; }
}