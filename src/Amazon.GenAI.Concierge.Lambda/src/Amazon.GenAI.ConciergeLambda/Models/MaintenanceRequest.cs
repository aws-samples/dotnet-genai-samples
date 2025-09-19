namespace Amazon.GenAI.ConciergeLambda.Models;

public class MaintenanceRequest
{
    public Guid RequestId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public IssueType IssueType { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public DateTime RequestDate { get; set; }
    public RequestStatus Status { get; set; }
}
