using System.Collections.Generic;
using System.Linq;

namespace Nexus.Sequencers
{
    public class ParallelStep : BaseStep
    {
        private List<IStep> childSteps = new List<IStep>();
        private bool allChildrenComplete => childSteps.All(seq => seq.IsComplete);
        private bool allChildrenFinished => childSteps.All(seq => seq.IsFinished);

        public override void InitStep()
        {
            base.InitStep();
            childSteps = GetComponentsInChildren<IStep>()
                .Where(seq => seq != this)
                .ToList();

            foreach (var step in childSteps)
            {
                step.InitStep();
            }
        }

        public override void StartStep()
        {
            base.StartStep();
            foreach (var step in childSteps)
            {
                step.StartStep();
            }
        }

        public override void UpdateStep()
        {
            if (!isComplete)
            {
                foreach (var step in childSteps)
                {
                    step.UpdateStep();
                }

                if (allChildrenComplete)
                {
                    Complete();
                }
            }
            
            if (isComplete && !isFinished && allChildrenFinished)
            {
                Finish();
            }
        }

        public override void CleanupStep()
        {
            foreach (var sequence in childSteps)
            {
                sequence.CleanupStep();
            }
            base.CleanupStep();
        }
    }
}