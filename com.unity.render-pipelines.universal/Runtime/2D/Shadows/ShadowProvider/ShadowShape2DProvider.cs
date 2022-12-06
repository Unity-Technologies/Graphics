using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal abstract class ShadowShape2DProvider
    {
        public virtual string ProviderName(string componentName) { return componentName; }
        public virtual int    Priority() { return 0; }
        public virtual void   Enabled(in Component sourceComponent) {}
        public virtual void   Disabled(in Component sourceComponent) {}
        public abstract bool  IsShapeSource(in Component sourceComponent);
        public abstract void  OnPersistantDataCreated(in Component sourceComponent, ShadowShape2D persistantShadowShape);
        public abstract void  OnBeforeRender(in Component sourceComponent, in Bounds worldCullingBounds, ShadowShape2D persistantShadowShape);
    }
}
