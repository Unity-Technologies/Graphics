using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public abstract class ShadowShape2DProvider : ScriptableObject
    {
        public virtual  int MenuPriority() { return 0; }
        public abstract bool CanProvideShape(in Component sourceComponent);
        public abstract void OnPersistantDataCreated(in Component sourceComponent, ShadowShape2D persistantShapeData);
        public abstract void OnBeforeRender(in Component sourceComponent, in Bounds worldCullingBounds, ShadowShape2D persistantShapeObject);
    }
}
