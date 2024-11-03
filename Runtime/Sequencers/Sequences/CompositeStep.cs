using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Sequencers
{
    public class CompositeStep : BaseStep
    {
        [Serializable]
        public class SequenceGroup
        {
            public List<BaseStep> sequences;
            public bool executeInParallel;
        }

        [SerializeField] private List<SequenceGroup> sequenceGroups;
        private int currentGroupIndex = -1;
        private ParallelStep parallelExecutor;
        private Sequence serialExecutor;

        protected virtual void Awake()
        {
            SetupExecutors();
        }

        private void SetupExecutors()
        {
            // Create parallel executor
            var parallelObj = new GameObject("ParallelExecutor");
            parallelObj.transform.SetParent(transform);
            parallelExecutor = parallelObj.AddComponent<ParallelStep>();

            // Create serial executor
            var serialObj = new GameObject("SerialExecutor");
            serialObj.transform.SetParent(transform);
            serialExecutor = serialObj.AddComponent<Sequence>();
        }

        public override void InitStep()
        {
            base.InitStep();
            currentGroupIndex = -1;
            PrepareNextGroup();
        }

        public override void StartStep()
        {
            base.StartStep();
            ExecuteCurrentGroup();
        }

        public override void UpdateStep()
        {
            if (!isComplete)
            {
                bool currentGroupComplete = false;
                
                if (sequenceGroups[currentGroupIndex].executeInParallel)
                {
                    parallelExecutor.UpdateStep();
                    currentGroupComplete = parallelExecutor.IsComplete;
                }
                else
                {
                    currentGroupComplete = serialExecutor.IsComplete;
                }

                if (currentGroupComplete)
                {
                    if (currentGroupIndex >= sequenceGroups.Count - 1)
                    {
                        Complete();
                        if (parallelExecutor.IsFinished)
                        {
                            Finish();
                        }
                    }
                    else
                    {
                        PrepareNextGroup();
                        ExecuteCurrentGroup();
                    }
                }
            }
        }

        private void PrepareNextGroup()
        {
            currentGroupIndex++;
            if (currentGroupIndex < sequenceGroups.Count)
            {
                var group = sequenceGroups[currentGroupIndex];
                
                // Clear previous executors
                foreach (Transform child in parallelExecutor.transform)
                    Destroy(child.gameObject);
                foreach (Transform child in serialExecutor.transform)
                    Destroy(child.gameObject);

                // Setup new sequences under appropriate executor
                foreach (var sequence in group.sequences)
                {
                    var sequenceInstance = Instantiate(sequence,
                        group.executeInParallel ? parallelExecutor.transform : serialExecutor.transform);
                    sequenceInstance.InitStep();
                }
            }
        }

        private void ExecuteCurrentGroup()
        {
            if (currentGroupIndex < sequenceGroups.Count)
            {
                var group = sequenceGroups[currentGroupIndex];
                if (group.executeInParallel)
                {
                    parallelExecutor.StartStep();
                }
                else
                {
                    serialExecutor.StartSequencer();
                }
            }
        }

        public override void CleanupStep()
        {
            parallelExecutor.CleanupStep();
            serialExecutor.Reset();
            base.CleanupStep();
        }
    }
}