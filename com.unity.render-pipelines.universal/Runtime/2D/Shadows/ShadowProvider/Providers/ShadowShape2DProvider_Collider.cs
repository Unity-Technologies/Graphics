#if USING_PHYSICS2D
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    class ShadowShape2DProvider_Collider2D : ShadowShape2DProvider
    {
        // Shadow state.
        struct MinMaxBounds
        {
            public Vector3 min;
            public Vector3 max;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Intersects(ref MinMaxBounds bounds)
            {
                return 
                    min.x <= bounds.max.x &&
                    max.x >= bounds.min.x &&
                    min.y <= bounds.max.y &&
                    max.y >= bounds.min.y &&
                    min.z <= bounds.max.z &&
                    max.z >= bounds.min.z;
            }

            public MinMaxBounds(ref Bounds bounds)
            {
                min = bounds.min;
                max = bounds.max;
            }
        };

        // Compare bounds.
        private static bool CompareApproximately(ref Bounds a, ref Bounds b)
        {
            return !(((a.min - b.min).sqrMagnitude > Mathf.Epsilon) || ((a.max - b.max).sqrMagnitude > Mathf.Epsilon));
        }

        private List<Bounds> m_ShadowShapeBounds;
        private List<MinMaxBounds> m_ShadowShapeMinMaxBounds;
        private MinMaxBounds m_ShadowCombinedShapeMinMaxBounds;
        private Bounds m_LastWorldCullingBounds;
        private bool m_ShadowDirty = true;
        private UInt32 m_ShadowStateHash = 0;
        private PhysicsShapeGroup2D m_ShadowShapeGroup;

        private void CalculateShadows(Collider2D collider, ShadowShape2D persistantShapeObject, Bounds worldCullingBounds)
        {
            // Create a shadow shape group if not available yet.
            if (m_ShadowShapeGroup == null)
                m_ShadowShapeGroup = new PhysicsShapeGroup2D(collider.shapeCount);

            // Create a shadow shape bounds if not available.
            if (m_ShadowShapeBounds == null)
                m_ShadowShapeBounds = new List<Bounds>(collider.shapeCount);

            // Create a shadow shape min/max bounds if not available.
            if (m_ShadowShapeMinMaxBounds == null)
                m_ShadowShapeMinMaxBounds = new List<MinMaxBounds>();

            // If the shape hash has changed, grab a new potential visible geometry group.
            var shapeHash = collider.GetShapeHash();
            if (shapeHash != m_ShadowStateHash)
            {
                // Set the current shadow shape hash.
                m_ShadowStateHash = shapeHash;

                // Clear the shapes.
                m_ShadowShapeGroup.Clear();

                // Finish if we don't have any shapes to return.
                if (collider.shapeCount == 0)
                {
                    // Indicate no shape data.
                    persistantShapeObject.SetShape(new NativeArray<Vector3>(), new NativeArray<int>(), ShadowShape2D.OutlineTopology.Lines, ShadowShape2D.WindingOrder.CounterClockwise);
                    return;
                }

                // Get shapes and finish if none are returned.
                if (collider.GetShapes(m_ShadowShapeGroup) == 0)
                    return;

                // Reset the last world culling bounds.
                m_LastWorldCullingBounds = default;

                // Fetch the shadow bounds.
                var combinedBounds = collider.GetShapeBounds(m_ShadowShapeBounds, useRadii: true, useWorldSpace: false);
                m_ShadowCombinedShapeMinMaxBounds = new MinMaxBounds(ref combinedBounds);

                // Calculate min/max bounds only once.               
                m_ShadowShapeMinMaxBounds.Clear();
                m_ShadowShapeMinMaxBounds.Capacity = m_ShadowShapeBounds.Capacity;
                for(var i = 0; i < m_ShadowShapeBounds.Count; ++i)
                {
                    var shapeBounds = m_ShadowShapeBounds[i];
                    m_ShadowShapeMinMaxBounds.Add(new MinMaxBounds(ref shapeBounds));
                }

                // Flag the shadow as dirty.
                m_ShadowDirty = true;
            }
            else
            {
                // Have the bounds changed?
                if (CompareApproximately(ref m_LastWorldCullingBounds, ref worldCullingBounds))
                {
                    // No, so finish.
                    return;
                }

                // Yes, so store the last world culling bounds.
                m_LastWorldCullingBounds = worldCullingBounds;

                // Flag the shadow as dirty.
                m_ShadowDirty = true;
            }

            // Finish if the shadow data hasn't changed or we have no shadow shapes.
            if (!m_ShadowDirty || m_ShadowShapeGroup.shapeCount == 0)
                return;

            // Fetch collider space.
            var colliderSpace = collider.LocalToWorld;

            // Calculate current collider-space culling bounds.
            var toColliderSpace = Matrix4x4.Inverse(colliderSpace);
            var cullCenter = worldCullingBounds.center + toColliderSpace.GetPosition();
            var worldCullExtents = worldCullingBounds.extents;
            var cullExtents = Vector3.zero;
            for (var i = 0; i < 3; ++i)
            {
                cullExtents[i] = Mathf.Abs(toColliderSpace[i, 0] * worldCullExtents.x) + Mathf.Abs(toColliderSpace[i, 1] * worldCullExtents.y) + Mathf.Abs(toColliderSpace[i, 2] * worldCullExtents.z);
            }
            worldCullingBounds = new Bounds(cullCenter, cullExtents * 2f);
            var worldCullingMinMaxBounds = new MinMaxBounds(ref worldCullingBounds);

            // Does the collider intersect the culling area at all?
            if (m_ShadowCombinedShapeMinMaxBounds.Intersects(ref worldCullingMinMaxBounds))
            {
                // Yes, so fetch a copy of the shapes and vertices.
                var shapeCount = m_ShadowShapeGroup.shapeCount;
                var shapeGroupShapes = m_ShadowShapeGroup.groupShapes;
                var shapeGroupVertices = m_ShadowShapeGroup.groupVertices;

                // Create visible shape indices.
                var visibleShapeIndices = new NativeArray<int>(shapeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                // We have to iterate the shapes to figure out both the vertex and index counts
                // because we're dealing with NativeArray which we have to specify a size for up-front.
                var vertexCount = 0;
                var indexCount = 0;
                var visibleShapeCount = 0;
                for (var i = 0; i < shapeCount; ++i)
                {
                    // Does this shape intersect the world culling bounds?
                    if (m_ShadowShapeMinMaxBounds[i].Intersects(ref worldCullingMinMaxBounds))
                    {
                        // Yes, so we can use it.
                        var shape = shapeGroupShapes[i];
                        var shapeVertexCount = shape.vertexCount;
                        var shapeType = shape.shapeType;

                        vertexCount += shapeVertexCount;

                        // Handle each specific shape type.
                        switch (shapeType)
                        {
                            case PhysicsShapeType2D.Circle:
                            case PhysicsShapeType2D.Capsule:
                                indexCount += 2;
                                break;

                            case PhysicsShapeType2D.Polygon:
                                indexCount += 2 * shapeVertexCount;
                                break;

                            case PhysicsShapeType2D.Edges:
                            {
                                // Calculate if this defines open edges i.e. the start/end vertex are not coincident.
                                var startVertex = shapeGroupVertices[shape.vertexStartIndex];
                                var endVertex = shapeGroupVertices[shape.vertexStartIndex + shape.vertexCount - 1];
                                var openEdges = (endVertex - startVertex).sqrMagnitude > Mathf.Epsilon;                              

                                // If we're open then we have one less index.
                                indexCount += 2 * (openEdges ? shapeVertexCount-1 : shapeVertexCount);
                                break;
                            }
                        }

                        // Add the shape to the visible shape indices.
                        visibleShapeIndices[visibleShapeCount++] = i;
                    }
                }
#if false
            //Debug.Log($"PVS: {shapeCount} / Vis: {visibleShapeCount}");
            //Debug.Log($"CENTER: {cullCenter} / Extents: {worldCullExtents}");
#endif
                // Do we have any visible shapes?
                if (visibleShapeCount > 0)
                {
                    // Yes, so create the shadow outputs.
                    var radii = new NativeArray<float>(vertexCount, Allocator.Temp);
                    var vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
                    var indices = new NativeArray<int>(indexCount, Allocator.Temp);

                    // Calculate transformation required to move the Collider geometry into shadow-space.
                    var toShadowSpace = colliderSpace * collider.transform.worldToLocalMatrix;

                    // Iterate all the visible shapes.
                    var vertexIndex = 0;
                    var indiceIndex = 0;
                    for (var i = 0; i < visibleShapeCount; ++i)
                    {
                        // Fetch the shape state.
                        var shape = shapeGroupShapes[visibleShapeIndices[i]];
                        var shapeType = shape.shapeType;
                        var radius = shape.radius;
                        var shapeVertexIndex = shape.vertexStartIndex;
                        var shapeVertexCount = shape.vertexCount;

                        // Handle each specific shape type.
                        switch (shapeType)
                        {
                            case PhysicsShapeType2D.Circle:
                                {
                                    radii[vertexIndex] = radius;
                                    indices[indiceIndex++] = vertexIndex;
                                    indices[indiceIndex++] = vertexIndex;
                                    vertices[vertexIndex++] = toShadowSpace.MultiplyPoint3x4(shapeGroupVertices[shapeVertexIndex]);
                                    break;
                                }

                            case PhysicsShapeType2D.Capsule:
                                {
                                    radii[vertexIndex] = radius;
                                    indices[indiceIndex++] = vertexIndex;
                                    vertices[vertexIndex++] = toShadowSpace.MultiplyPoint3x4(shapeGroupVertices[shapeVertexIndex++]);
                                    radii[vertexIndex] = radius;
                                    indices[indiceIndex++] = vertexIndex;
                                    vertices[vertexIndex++] = toShadowSpace.MultiplyPoint3x4(shapeGroupVertices[shapeVertexIndex++]);
                                    break;
                                }

                            case PhysicsShapeType2D.Polygon:
                                {
                                    var startIndex = vertexIndex;
                                    var edgeIndex = vertexIndex;
                                    for (var n = 0; n < (shapeVertexCount - 1); ++n)
                                    {
                                        radii[vertexIndex] = radius;
                                        vertices[vertexIndex++] = toShadowSpace.MultiplyPoint3x4(shapeGroupVertices[shapeVertexIndex++]);
                                        indices[indiceIndex++] = edgeIndex++;
                                        indices[indiceIndex++] = edgeIndex;
                                    }
                                    radii[vertexIndex] = radius;
                                    vertices[vertexIndex++] = toShadowSpace.MultiplyPoint3x4(shapeGroupVertices[shapeVertexIndex++]);
                                    indices[indiceIndex++] = edgeIndex;
                                    indices[indiceIndex++] = startIndex;
                                    break;
                                }

                            case PhysicsShapeType2D.Edges:
                                {
                                    var startIndex = vertexIndex;
                                    var edgeIndex = vertexIndex;
                                    for (var n = 0; n < (shapeVertexCount - 1); ++n)
                                    {
                                        radii[vertexIndex] = radius;
                                        vertices[vertexIndex++] = toShadowSpace.MultiplyPoint3x4(shapeGroupVertices[shapeVertexIndex++]);
                                        indices[indiceIndex++] = edgeIndex++;
                                        indices[indiceIndex++] = edgeIndex;
                                    }
                                    radii[vertexIndex] = radius;
                                    vertices[vertexIndex++] = toShadowSpace.MultiplyPoint3x4(shapeGroupVertices[shapeVertexIndex++]);

                                    // Calculate if this defines open edges i.e. the start/end vertex are not coincident.
                                    // NOTE: We're forced to calculate this again because we have no way to persist this from the index counting earlier.
                                    var startVertex = shapeGroupVertices[shape.vertexStartIndex];
                                    var endVertex = shapeGroupVertices[shape.vertexStartIndex + shape.vertexCount - 1];
                                    var openEdges = (endVertex - startVertex).sqrMagnitude > Mathf.Epsilon;
                                    if (!openEdges)
                                    {
                                        indices[indiceIndex++] = edgeIndex;
                                        indices[indiceIndex++] = startIndex;
                                    }

                                    break;
                                }
                        }
                    }

                    // Set the shadow shape.
                    persistantShapeObject.SetShape(vertices, indices, radii, ShadowShape2D.WindingOrder.CounterClockwise);

                    // Shadow is no longer dirty.
                    m_ShadowDirty = true;

                    // Clean up.
                    radii.Dispose();
                    indices.Dispose();
                    vertices.Dispose();
                }

                // Clean up.
                visibleShapeIndices.Dispose();
            }
        }

        //============================================================================================================
        //                                                  Public
        //============================================================================================================
        public override bool IsShapeSource(in Component sourceComponent) { return sourceComponent is Collider2D; }

        public override void OnPersistantDataCreated(in Component sourceComponent, ShadowShape2D persistantShapeData)
        {
            m_ShadowStateHash = 0;
            m_ShadowCombinedShapeMinMaxBounds = default;
        }

        public override void OnBeforeRender(in Component sourceComponent, in Bounds worldCullingBounds, ShadowShape2D persistantShapeObject)
        {
            Collider2D collider = (Collider2D)sourceComponent;
            CalculateShadows(collider, persistantShapeObject, worldCullingBounds);
        }

   }
}
#endif
