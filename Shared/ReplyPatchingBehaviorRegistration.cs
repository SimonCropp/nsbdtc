using NServiceBus.Pipeline;

class ReplyPatchingBehaviorRegistration :
    RegisterStep
{
    public ReplyPatchingBehaviorRegistration(string endpointName)
        : base(
            stepId: "ReplyPatchingBehavior",
            behavior: typeof(ReplyPatchingBehavior),
            description: "Fixes the reply address to be NSB",
            factoryMethod: _ => new ReplyPatchingBehavior(endpointName))
    {
    }
}