using Amazon.Lambda.Core;

namespace Amazon.GenAI.KbLambda;

public  class LambdaBaseFunction
{
    protected static ILambdaContext? Context;

    public static void SetContext(ILambdaContext? context) => Context = context;
}