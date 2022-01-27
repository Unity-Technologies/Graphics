using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct CallMethod : UIDefinition.IFeatureParameter
    {
        public readonly Action call;
    }
}
