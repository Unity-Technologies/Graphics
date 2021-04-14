using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Path2D
{
    internal enum TangentMode
    {
        Linear = 0,
        Continuous = 1,
        Broken = 2
    }

    [Serializable]
    internal struct TangentCache
    {
        public Vector3 leftTangent;
        public Vector3 rightTangent;
    }

    [Serializable]
    internal struct ControlPoint
    {
        public Vector3 position;
        public Vector3 localLeftTangent;
        public Vector3 localRightTangent;
        public TangentMode tangentMode;
        public TangentCache continuousCache;
        public TangentCache brokenCache;
        public bool mirrorLeft;

        public ControlPoint(Vector3 pos)
        {
            position = pos;
            localLeftTangent = Vector3.zero;
            localRightTangent = Vector3.zero;
            tangentMode = TangentMode.Linear;
            continuousCache = default(TangentCache);
            brokenCache = default(TangentCache);
            mirrorLeft = false;
        }

        public Vector3 leftTangent
        {
            get { return localLeftTangent + position; }
            set { localLeftTangent = value - position; }
        }

        public Vector3 rightTangent
        {
            get { return localRightTangent + position; }
            set { localRightTangent = value - position; }
        }

        public void StoreTangents()
        {
            if (tangentMode == TangentMode.Continuous)
            {
                continuousCache.leftTangent = localLeftTangent;
                continuousCache.rightTangent = localRightTangent;
            }
            else if (tangentMode == TangentMode.Broken)
            {
                brokenCache.leftTangent = localLeftTangent;
                brokenCache.rightTangent = localRightTangent;
            }
        }

        public void RestoreTangents()
        {
            if (tangentMode == TangentMode.Continuous)
            {
                localLeftTangent = continuousCache.leftTangent;
                localRightTangent = continuousCache.rightTangent;
            }
            else if (tangentMode == TangentMode.Broken)
            {
                localLeftTangent = brokenCache.leftTangent;
                localRightTangent = brokenCache.rightTangent;
            }
        }
    }
}
