using System;

namespace Amazon.GenAI.Cdk;

public class AppStackProps
{
    public string LogLevel { get; set; } = "INFO";
    public string NamePrefix { get; set; } = Constants.AppName;
    public string NameSuffix { get; set; } = Constants.ToCurrentDateTime();
}