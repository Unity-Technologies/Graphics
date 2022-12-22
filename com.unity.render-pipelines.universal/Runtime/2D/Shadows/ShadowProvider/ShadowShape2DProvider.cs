using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class <c>ShadowShape2DProvider</c> has methods called by a <c>ShadowCaster2D</c> to determine if it should be listed as a Casting Option, and to provide geometry if it is the active <c>ShadowShape2DProvider</c>
    /// </summary>
    [Serializable]
    public abstract class ShadowShape2DProvider
    {
        /// <summary>
        /// Gets the name to be listed in the <c>ShadowCaster2D</c> Casting Option drop down.
        /// </summary>
        /// <param name="componentName">The name of component associated with the provider.</param>
        /// <returns>The string to be listed in the <c>ShadowCaster2D</c> Casting Option drop down.</returns>
        public virtual string ProviderName(string componentName) { return componentName; }

        /// <summary>
        /// Gets the priority to be listed in the <c>ShadowCaster2D</c> Casting Option drop down.
        /// </summary>
        /// <returns>The priority to be listed in the <c>ShadowCaster2D</c> Casting Option drop down.</returns>
        public virtual int    Priority() { return 0; }

        /// <summary>
        /// Called for the active <c>ShadowShape2DProvider</c> when the <c>ShadowCaster2D</c> becomes enabled
        /// </summary>
        /// <param name="sourceComponent">The component associated with the provider</param>
        public virtual void   Enabled(in Component sourceComponent) {}

        /// <summary>
        /// Called for the active <c>ShadowShape2DProvider</c> when the <c>ShadowCaster2D</c> becomes disabled
        /// </summary>
        /// <param name="sourceComponent">The component associated with the provider</param>
        public virtual void   Disabled(in Component sourceComponent) {}

        /// <summary>
        /// Called for each component on a <c>ShadowCaster2D's</c> <c>GameObject</c>. Returns true if the provided component is the data source of the <c>ShadowShapeProvider</c>.
        /// </summary>
        /// <param name="sourceComponent">The component to test as a source</param>
        /// <returns>Returns true if sourceComponent is the data source of the <c>ShadowShapeProvider</c>.</returns>
        public abstract bool  IsShapeSource(in Component sourceComponent);

        /// <summary>
        /// Called when the <c>ShadowShape2DProvider</c> is selected as the active Casting Option.
        /// </summary>
        /// <param name="sourceComponent">The component associated with the provider</param>
        /// <param name="persistantShadowShape">An instance of <c>ShadowShape2D</c> that is used by the <c>ShadowCaster2D</c></param>
        public abstract void  OnPersistantDataCreated(in Component sourceComponent, ShadowShape2D persistantShadowShape);

        /// <summary>
        /// Called before 2D lighting is rendered each frame
        /// </summary>
        /// <param name="sourceComponent">The component associated with the provider</param>
        /// <param name="worldCullingBounds">The bounds enclosing the region of the view frustum and all visible lights</param>
        /// <param name="persistantShadowShape">An instance of <c>ShadowShape2D</c> that is used by the <c>ShadowCaster2D</c></param>
        public abstract void  OnBeforeRender(in Component sourceComponent, in Bounds worldCullingBounds, ShadowShape2D persistantShadowShape);
    }
}
