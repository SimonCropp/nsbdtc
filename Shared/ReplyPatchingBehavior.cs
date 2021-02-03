using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

class ReplyPatchingBehavior :
    Behavior<IOutgoingPhysicalMessageContext>
{
    string endpointName;

    public ReplyPatchingBehavior(string endpointName)
    {
        this.endpointName = endpointName;
    }
    public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
    {
        context.Headers["NServiceBus.ReplyToAddress"] = $"{endpointName}@[dbo]@[NServiceBus]";
        return next();
    }
}