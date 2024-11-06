using UnityEngine;

namespace Paths.TwoD
{
    public interface IPath
    {
        Vector2 Evaluate(float t);
        float GetLength();
        float GetParameterAtDistance(float distance);
    }
}