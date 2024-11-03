namespace Nexus.Sequences
{
    public interface IStepWithContext : IStep
    {
        void SetContext(StepContext context);
    }
}