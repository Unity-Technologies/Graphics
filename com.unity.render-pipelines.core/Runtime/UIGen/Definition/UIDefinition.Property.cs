using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIDefinition
    {
        public class Property
        {
            List<IFeatureParameter> featureParameters = new();
            Property parent;

            /// <summary>
            /// path from context root to this property, member access separated by `.`
            /// </summary>
            public string propertyPath { get; set; }
            public Type type { get; }
        }
    }
}
