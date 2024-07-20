using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.GenAI;

public class ApiInitializerService()
{
    public async Task InitializeAsync()
    {
        var client = new AmazonEC2Client();

        var request = new DescribeAvailabilityZonesRequest();
        var response = await client.DescribeAvailabilityZonesAsync(request);

        if(response.AvailabilityZones.Count > 0)
        {
            AvailabilityZone? firstZone = response.AvailabilityZones[0];
            Constants.Region = firstZone.RegionName;
            Console.WriteLine($"Region Name: {firstZone.RegionName}");
        }
        else
        {
            Console.WriteLine("No availability zones found.");
        }
    }
}