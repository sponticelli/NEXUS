using System;
using UnityEngine;

namespace Nexus.Extensions
{
    public static class PlayerPrefsExtensions
    {
        public static void SetVector3(this Vector3 vector, string key)
        {
            PlayerPrefs.SetFloat($"{key}_x", vector.x);
            PlayerPrefs.SetFloat($"{key}_y", vector.y);
            PlayerPrefs.SetFloat($"{key}_z", vector.z);
        }

        public static Vector3 GetVector3(string key, Vector3 defaultValue = default)
        {
            if (!PlayerPrefs.HasKey($"{key}_x"))
                return defaultValue;

            return new Vector3(
                PlayerPrefs.GetFloat($"{key}_x"),
                PlayerPrefs.GetFloat($"{key}_y"),
                PlayerPrefs.GetFloat($"{key}_z")
            );
        }

        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) == 1 : defaultValue;
        }

        public static void SetEnum<T>(string key, T value) where T : Enum
        {
            PlayerPrefs.SetInt(key, Convert.ToInt32(value));
        }

        public static T GetEnum<T>(string key, T defaultValue) where T : Enum
        {
            return PlayerPrefs.HasKey(key) 
                ? (T)Enum.ToObject(typeof(T), PlayerPrefs.GetInt(key)) 
                : defaultValue;
        }
    }
}