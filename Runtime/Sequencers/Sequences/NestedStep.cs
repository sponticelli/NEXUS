using UnityEngine;

namespace Nexus.Sequencers
{
    public class NestedStep : BaseStep
    {
        [SerializeField] private bool autoStart = true;
        private Sequence childSequence;
        
        protected virtual void Awake()
        {
            childSequence = GetComponentInChildren<Sequence>();
            if (childSequence == null)
            {
                var sequencerObj = new GameObject("NestedSequencer");
                sequencerObj.transform.SetParent(transform);
                childSequence = sequencerObj.AddComponent<Sequence>();
            }

            childSequence.OnCompleteEvent += HandleCompleteEvent;
        }

        protected virtual void OnDestroy()
        {
            if (childSequence != null)
            {
                childSequence.OnCompleteEvent -= HandleCompleteEvent;
            }
        }

        public override void InitStep()
        {
            base.InitStep();
            childSequence.Initialize();
        }

        public override void StartStep()
        {
            base.StartStep();
            if (autoStart)
            {
                childSequence.StartSequencer();
            }
        }

        public override void UpdateStep()
        {
            // The child sequencer handles its own updating through Unity's Update
        }

        private void HandleCompleteEvent()
        {
            Complete();
            Finish();
        }

        public override void CleanupStep()
        {
            childSequence.Reset();
            base.CleanupStep();
        }

        public void StartChildSequencer()
        {
            childSequence.StartSequencer();
        }

        public void PauseChildSequencer()
        {
            childSequence.Pause();
        }

        public void ResumeChildSequencer()
        {
            childSequence.Resume();
        }
    }
}