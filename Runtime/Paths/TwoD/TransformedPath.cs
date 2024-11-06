using System;
using UnityEngine;

namespace Paths.TwoD
{
    public class TransformedPath : IPath
    {
        private readonly IPath originalPath;
        private readonly Func<Vector2, Vector2> transformation;

        public TransformedPath(IPath originalPath, Func<Vector2, Vector2> transformation)
        {
            this.originalPath = originalPath;
            this.transformation = transformation;
        }

        public Vector2 Evaluate(float t) => transformation(originalPath.Evaluate(t));
        public float GetLength() => originalPath.GetLength();
        public float GetParameterAtDistance(float distance) => originalPath.GetParameterAtDistance(distance);
    }
}