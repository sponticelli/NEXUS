using UnityEngine;

namespace Nexus.Pooling
{
    public class ReturnToPoolAfterDelay : MonoBehaviour
    {
        [SerializeField] private float _delay = 1f;
        
        private float _timer;
        private bool _triggered;
        
        private void OnEnable()
        {
            _timer = 0f;
            _triggered = false;
        }
        
        private void Update()
        {
            if (_triggered) return;
            
            _timer += Time.deltaTime;
            if (!(_timer >= _delay)) return;
            
            _triggered = true;
            gameObject.ReturnToPool();
        }
    }
}