using UnityEngine;

namespace Nexus.Sequencers
{
    public class TimedStep : BaseStep
    {
        public float duration = 1f;
        private float timer;

        public override void StartStep()
        {
            base.StartStep();
            timer = 0f;
        }

        public override void UpdateStep()
        {
            if (!isComplete)
            {
                timer += Time.deltaTime;
                if (timer >= duration)
                {
                    Complete();
                    Finish();
                }
            }
        }
    }
}