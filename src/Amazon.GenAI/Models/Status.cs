namespace Amazon.GenAI.Models;

public enum Status
{
    Default,
    Thinking,
    Indexing,
    Adding
}

public enum ActionType
{
    Searching,
    Adding,
    S3Adding
}

public enum ModalType
{
	Text,
	Image,
	TextAndImage
}