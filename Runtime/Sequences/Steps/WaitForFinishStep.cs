using System;

namespace Nexus.Sequences
{
    public class WaitForFinishStep : BaseStep
    {
        private IStep previousStep;

        public override void InitStep()
        {
            base.InitStep();
            previousStep = GetValidPreviousSequence();
        }

        public override void StartStep()
        {
            base.StartStep();
            if (previousStep == null)
            {
                Complete();
                Finish();
                return;
            }
        }

        public override void UpdateStep()
        {
            if (!isComplete && previousStep != null && previousStep.IsFinished)
            {
                Complete();
                Finish();
            }
        }

        private IStep GetValidPreviousSequence()
        {
            var sequences = transform.parent.GetComponentsInChildren<IStep>();
            int currentIndex = Array.IndexOf(sequences, this);
            return currentIndex > 0 ? sequences[currentIndex - 1] : null;
        }
    }
}