using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

class ReplyPatchingBehavior :
    Behavior<IOutgoingPhysicalMessageContext>
{
    string endpointName;

    ReplyPatchingBehavior(string endpointName)
    {
        this.endpointName = endpointName;
    }

    public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
    {
        context.Headers["NServiceBus.ReplyToAddress"] = $"{endpointName}@[dbo]@[NServiceBus]";
        return next();
    }

    public class Step :
        RegisterStep
    {
        public Step(string endpointName)
            : base(
                stepId: "ReplyPatchingBehavior",
                behavior: typeof(ReplyPatchingBehavior),
                description: "Fixes the reply address to be NSB",
                factoryMethod: _ => new ReplyPatchingBehavior(endpointName))
        {
        }

    }
}