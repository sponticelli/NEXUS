using System.Collections.Generic;
using System.Linq;

namespace Nexus.Sequencers
{
    public class ParallelStep : BaseStep
    {
        private List<IStep> childSequences = new List<IStep>();
        private bool allChildrenComplete => childSequences.All(seq => seq.IsComplete);
        private bool allChildrenFinished => childSequences.All(seq => seq.IsFinished);

        public override void InitStep()
        {
            base.InitStep();
            childSequences = GetComponentsInChildren<IStep>()
                .Where(seq => seq != this)
                .ToList();

            foreach (var sequence in childSequences)
            {
                sequence.InitStep();
            }
        }

        public override void StartStep()
        {
            base.StartStep();
            foreach (var sequence in childSequences)
            {
                sequence.StartStep();
            }
        }

        public override void UpdateStep()
        {
            if (!isComplete)
            {
                foreach (var sequence in childSequences)
                {
                    sequence.UpdateStep();
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
            foreach (var sequence in childSequences)
            {
                sequence.CleanupStep();
            }
            base.CleanupStep();
        }
    }
}