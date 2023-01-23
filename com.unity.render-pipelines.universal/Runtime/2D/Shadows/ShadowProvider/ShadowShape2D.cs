using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class <c>ShadowShape2D</c> stores outline geometry for use with a shadow caster.
    /// </summary>
    public abstract class ShadowShape2D
    {
        /// <summary>
        /// Used when calling SetShape to describe the supplied indicies
        /// </summary>
        public enum OutlineTopology
        {
            /// <summary>
            /// Describes an index array where a set of two consecutive points describes a line segment.
            /// </summary>
            Lines,
            /// <summary>
            /// Describes an index array where a set of three consecutive points describes a triangle.
            /// </summary>
            Triangles
        }

        /// <summary>
        /// Used when calling SetShape to describe the winding direction of the supplied geometry
        /// </summary>
        public enum WindingOrder
        {
            /// <summary>
            /// Describes an index array where all polygons have vertices in a clockwise order
            /// </summary>
            Clockwise,
            /// <summary>
            /// Describes an index array where all polygons have vertices in a counterclockwise order
            /// </summary>
            CounterClockwise
        }

        /// <summary>
        /// SetFlip specifies how the shadow shape should be flipped when rendered
        /// </summary>
        /// <param name="flipX"> Specifies flipping on the local x-axis </param>
        /// <param name="flipY"> Specifies flipping on the local y-axis </param>
        public abstract void SetFlip(bool flipX, bool flipY);

        /// <summary>
        /// GetFlip returns how the shadow shape should be flipped when rendered
        /// </summary>
        /// <param name="flipX"> Returns flipping on the local x-axis </param>
        /// <param name="flipY"> Returns flipping on the local y-axis </param>
        public abstract void GetFlip(out bool flipX, out bool flipY);

        /// <summary>
        /// The value to initialize the trim when created
        /// </summary>
        /// <param name="trim"> Specifies the starting trim value.</param>
        public abstract void SetDefaultTrim(float trim);

        /// <summary>
        /// SetShape creates shadow geometry using the supplied geometry
        /// </summary>
        /// <param name="vertices">The vertices used to create the shadow geometry.</param>
        /// <param name="indices">The indices used to create the shadow geometry (Lines topology) </param>
        /// <param name="radii">The radius at the vertex. Can be used to describe a capsule.</param>
        /// <param name="transform"> The transform used to create the shadow geometry.</param>
        /// <param name="windingOrder">The winding order of the supplied geometry.</param>
        /// <param name="allowContraction">Specifies if the ShadowCaster2D is allowed to contract the supplied shape(s).</param>
        /// <param name="createInteriorGeometry">Specifies if the ShadowCaster2D should create interior geometry. Required for shadow casters that do not use renderers as their source.</param>
        public abstract void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, NativeArray<float> radii, Matrix4x4 transform, WindingOrder windingOrder = WindingOrder.Clockwise, bool allowContraction = true, bool createInteriorGeometry = false);

        /// <summary>
        /// SetShape creates shadow geometry using the supplied geometry
        /// </summary>
        /// <param name="vertices">The vertices used to create the shadow geometry.</param>
        /// <param name="indices">The indices used to create the shadow geometry (Lines topology) </param>
        /// <param name="outlineTopology">The settings to create the renderer with.</param>
        /// <param name="windingOrder">The winding order of the supplied geometry.</param>
        /// <param name="allowContraction">Specifies if the ShadowCaster2D is allowed to contract the supplied shape(s).</param>
        /// <param name="createInteriorGeometry">Specifies if the ShadowCaster2D should create interior geometry. Required for shadow casters that do not use renderers as their source.</param>
        public abstract void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, OutlineTopology outlineTopology, WindingOrder windingOrder = WindingOrder.Clockwise, bool allowContraction = true, bool createInteriorGeometry = false);
    }
}
