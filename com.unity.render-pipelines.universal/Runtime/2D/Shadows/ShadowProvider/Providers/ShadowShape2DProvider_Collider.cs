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
        private List<Bounds> m_ShadowShapeBounds;
        private List<MinMaxBounds> m_ShadowShapeMinMaxBounds;
        private MinMaxBounds m_ShadowCombinedShapeMinMaxBounds;
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
            }

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
                var visibleShapeIndices = new NativeArray<int>(shapeCount, Allocator.Temp, NativeArrayOptions.ClearMemory);

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
                                indexCount += 2 * (shapeVertexCount - 1);
                                break;
                        }

                        // Add the shape to the visible shape indices.
                        visibleShapeIndices[visibleShapeCount++] = i;
                    }
                }
#if false
            Debug.Log($"PVS: {shapeCount} / Vis: {visibleShapeCount}");
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
                                    break;
                                }
                        }
                    }

                    // Set the shadow shape.
                    persistantShapeObject.SetShape(vertices, indices, radii, ShadowShape2D.WindingOrder.CounterClockwise);

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
        public override bool CanProvideShape(in Component sourceComponent) { return sourceComponent is Collider2D; }

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
