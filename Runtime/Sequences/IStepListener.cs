namespace Nexus.Sequences
{
    public interface IStepListener
    {
        void OnStepComplete();
        void OnStepFinished();
    }
}