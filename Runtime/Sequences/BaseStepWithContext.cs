namespace Nexus.Sequences
{
    public class BaseStepWithContext : BaseStep, IStepWithContext
    {
        protected StepContext context;

        public StepContext Context => context;
        
        

        public void SetContext(StepContext context)
        {
            this.context = context;
        }
    }
}