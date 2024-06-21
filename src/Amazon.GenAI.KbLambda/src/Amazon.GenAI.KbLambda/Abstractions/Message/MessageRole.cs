namespace Amazon.GenAI.KbLambda.Abstractions.Message;

public enum MessageRole
{
    System,
    Human,
    Ai,
    Chat,
    FunctionCall,
    FunctionResult
}
