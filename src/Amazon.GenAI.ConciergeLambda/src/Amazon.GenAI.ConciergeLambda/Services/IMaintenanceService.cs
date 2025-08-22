using Amazon.GenAI.ConciergeLambda.Models;

namespace Amazon.GenAI.ConciergeLambda.Services;

public interface IMaintenanceService
{
    Task<Guid> CreateMaintenanceRequestAsync(string guestName, string roomNumber, IssueType issueType, string issueDescription, Priority? priority = null);
}
