using UnityEngine;

namespace Nexus.Core.Rx.Unity
{
    public class GameProperty<T> : ReactiveProperty<T>
    {
        private readonly string key;
        private readonly bool persistent;

        public GameProperty(T initialValue, string saveKey = null) : base(initialValue)
        {
            key = saveKey;
            persistent = !string.IsNullOrEmpty(key);
                
            if (persistent && PlayerPrefs.HasKey(key))
            {
                // Load saved value if available
                LoadValue();
            }
        }

        public override T Value
        {
            get => base.Value;
            set
            {
                base.Value = value;
                if (persistent)
                {
                    SaveValue();
                }
            }
        }

        protected virtual void SaveValue()
        {
            // Basic type handling - expand as needed
            if (typeof(T) == typeof(int))
                PlayerPrefs.SetInt(key, (int)(object)Value);
            else if (typeof(T) == typeof(float))
                PlayerPrefs.SetFloat(key, (float)(object)Value);
            else if (typeof(T) == typeof(string))
                PlayerPrefs.SetString(key, (string)(object)Value);
                
            PlayerPrefs.Save();
        }

        protected virtual void LoadValue()
        {
            if (typeof(T) == typeof(int))
                Value = (T)(object)PlayerPrefs.GetInt(key);
            else if (typeof(T) == typeof(float))
                Value = (T)(object)PlayerPrefs.GetFloat(key);
            else if (typeof(T) == typeof(string))
                Value = (T)(object)PlayerPrefs.GetString(key);
        }
    }
}