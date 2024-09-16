namespace Amazon.GenAI.Abstractions.ChatHistory;

/// <summary>
/// In memory implementation of chat message history.
/// 
/// Stores messages in an in memory list.
/// </summary>
public class ChatMessageHistory : BaseChatMessageHistory
{
    private readonly List<Message.Message> _messages = new List<Message.Message>();

    /// <summary>
    /// Used to inspect and filter messages on their way to the history store
    /// NOTE: This is not a feature of python langchain
    /// </summary>
    public Predicate<Message.Message> IsMessageAccepted { get; set; } = x => true;

    /// <inheritdoc/>
    public override IReadOnlyList<Message.Message> Messages => _messages;

    /// <inheritdoc/>
    public override Task AddMessage(Message.Message message)
    {
        if (IsMessageAccepted(message))
        {
            message = message with { DateTime = DateTime.Now };
            _messages.Add(message);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task Clear()
    {
        _messages.Clear();
        return Task.CompletedTask;
    }

    public Task AddAiMessage(string text)
    {
        var message = new Message.Message();
        if (IsMessageAccepted(message))
        {
            message = message with { Content = text };
            message = message with { DateTime = DateTime.Now };
            _messages.Add(message);
        }

        return Task.CompletedTask;
    }

    public  Task AddAiMessage(Message.Message? message)
    {
        if (IsMessageAccepted(message.Value))
        {
            message = message.Value with { Content = message.Value.Content };
            message = message.Value with { DateTime = DateTime.Now };
            _messages.Add(message.Value);
        }

        return Task.CompletedTask;
    }
}