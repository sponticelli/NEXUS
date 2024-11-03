namespace Nexus.Sequencers
{
    public interface IStepWithContext : IStep
    {
        void SetContext(StepContext context);
    }
}