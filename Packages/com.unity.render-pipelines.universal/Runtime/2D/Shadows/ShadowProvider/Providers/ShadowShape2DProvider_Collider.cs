#if USING_PHYSICS2D
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    class ShadowShape2DProvider_Collider2D : ShadowShape2DProvider
    {
        const float k_InitialTrim = 0.05f;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CompareApproximately(ref Bounds a, ref Bounds b)
        {
            return !(((a.min - b.min).sqrMagnitude > Mathf.Epsilon) || ((a.max - b.max).sqrMagnitude > Mathf.Epsilon));
        }

        // Rotate bounds.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TransformBounds2D(Matrix4x4 transform, ref Bounds bounds)
        {
            var center = transform.MultiplyPoint(bounds.center);
            var extents = bounds.extents;

            var axisX = transform.MultiplyVector(new Vector3(extents.x, 0f, 0f));
            var axisY = transform.MultiplyVector(new Vector3(0f, extents.y, 0f));

            extents.x = MathF.Abs(axisX.x) + MathF.Abs(axisY.x);
            extents.y = MathF.Abs(axisX.y) + MathF.Abs(axisY.y);

            bounds = new Bounds { center = center, extents = extents };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClearShapes(ShadowShape2D persistantShapeObject)
        {
            // Indicate no shape data.
            persistantShapeObject.SetShape(new NativeArray<Vector3>(), new NativeArray<int>(), ShadowShape2D.OutlineTopology.Lines, ShadowShape2D.WindingOrder.CounterClockwise);
        }

        private List<Bounds> m_ShadowShapeBounds;
        private List<MinMaxBounds> m_ShadowShapeMinMaxBounds;
        private MinMaxBounds m_ShadowCombinedShapeMinMaxBounds;
        private Bounds m_LastWorldCullingBounds;
        private Matrix4x4 m_LastColliderSpace;
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

            // Fetch collider space.
            var attachedBody = collider.attachedRigidbody;
            var colliderSpace = attachedBody ? attachedBody.transform.localToWorldMatrix : Matrix4x4.identity;

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
                    // Clear the shapes.
                    ClearShapes(persistantShapeObject);
                    return;
                }

                // Get shapes and finish if none are returned.
                if (collider.GetShapes(m_ShadowShapeGroup) == 0)
                    return;

                // Set the last world culling bounds.
                m_LastWorldCullingBounds = worldCullingBounds;

                // Fetch the shadow bounds.
                var combinedBounds = collider.GetShapeBounds(m_ShadowShapeBounds, useRadii: true, useWorldSpace: false);
                m_ShadowCombinedShapeMinMaxBounds = new MinMaxBounds(ref combinedBounds);

                // Calculate min/max bounds only once.
                m_ShadowShapeMinMaxBounds.Clear();
                m_ShadowShapeMinMaxBounds.Capacity = m_ShadowShapeBounds.Capacity;
                for (var i = 0; i < m_ShadowShapeBounds.Count; ++i)
                {
                    var shapeBounds = m_ShadowShapeBounds[i];
                    m_ShadowShapeMinMaxBounds.Add(new MinMaxBounds(ref shapeBounds));
                }

                // Flag the shadow as dirty.
                m_ShadowDirty = true;
            }
            else
            {
                // If the collider space nor the culling bounds have not changed then finish.
                if (colliderSpace.Equals(m_LastColliderSpace) &&
                    CompareApproximately(ref m_LastWorldCullingBounds, ref worldCullingBounds))
                {
                    // No, so finish.
                    return;
                }

                // Yes, so store the last world culling bounds.
                m_LastWorldCullingBounds = worldCullingBounds;

                // Flag the shadow as dirty.
                m_ShadowDirty = true;
            }

            // Store the last collider space.
            m_LastColliderSpace = colliderSpace;

            // Finish if the shadow data hasn't changed or we have no shadow shapes.
            if (!m_ShadowDirty || m_ShadowShapeGroup.shapeCount == 0)
                return;

            // Shadow is no longer going to be dirty.
            m_ShadowDirty = false;

            // Does the collider intersect the culling area at all?
            TransformBounds2D(Matrix4x4.Inverse(colliderSpace), ref worldCullingBounds);
            var worldCullingMinMaxBounds = new MinMaxBounds(ref worldCullingBounds);
            if (!m_ShadowCombinedShapeMinMaxBounds.Intersects(ref worldCullingMinMaxBounds))
            {
                // No, so clear the shapes.
                ClearShapes(persistantShapeObject);
                return;
            }

            // Fetch a copy of the shapes and vertices.
            var shapeCount = m_ShadowShapeGroup.shapeCount;
            var shapeGroupShapes = m_ShadowShapeGroup.groupShapes;
            var shapeGroupVertices = m_ShadowShapeGroup.groupVertices;

            // Create visible shape indices.
            var visibleShapeIndices = new NativeArray<int>(shapeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // We have to iterate the shapes to figure out both the vertex and index counts
            // because we're dealing with NativeArray which we have to specify a size for up-front.ebug
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
                            indexCount += 2 * (openEdges ? shapeVertexCount - 1 : shapeVertexCount);
                            break;
                        }
                    }

                    // Add the shape to the visible shape indices.
                    visibleShapeIndices[visibleShapeCount++] = i;
                }
            }

            // Do we have any visible shapes?
            if (visibleShapeCount > 0)
            {
                // Yes, so create the shadow outputs.
                var radii = new NativeArray<float>(vertexCount, Allocator.Temp);
                var vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
                var indices = new NativeArray<int>(indexCount, Allocator.Temp);

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
                            vertices[vertexIndex++] = shapeGroupVertices[shapeVertexIndex];
                            break;
                        }

                        case PhysicsShapeType2D.Capsule:
                        {
                            radii[vertexIndex] = radius;
                            indices[indiceIndex++] = vertexIndex;
                            vertices[vertexIndex++] = shapeGroupVertices[shapeVertexIndex++];
                            radii[vertexIndex] = radius;
                            indices[indiceIndex++] = vertexIndex;
                            vertices[vertexIndex++] = shapeGroupVertices[shapeVertexIndex++];
                            break;
                        }

                        case PhysicsShapeType2D.Polygon:
                        {
                            var startIndex = vertexIndex;
                            var edgeIndex = vertexIndex;
                            for (var n = 0; n < (shapeVertexCount - 1); ++n)
                            {
                                radii[vertexIndex] = radius;
                                vertices[vertexIndex++] = shapeGroupVertices[shapeVertexIndex++];
                                indices[indiceIndex++] = edgeIndex++;
                                indices[indiceIndex++] = edgeIndex;
                            }
                            radii[vertexIndex] = radius;
                            vertices[vertexIndex++] = shapeGroupVertices[shapeVertexIndex++];
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
                                vertices[vertexIndex++] = shapeGroupVertices[shapeVertexIndex++];
                                indices[indiceIndex++] = edgeIndex++;
                                indices[indiceIndex++] = edgeIndex;
                            }
                            radii[vertexIndex] = radius;
                            vertices[vertexIndex++] = shapeGroupVertices[shapeVertexIndex++];

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

                // Calculate transformation required to move the Collider geometry into shadow-space.
                var toShadowSpace = collider.transform.worldToLocalMatrix * colliderSpace;


                Renderer renderer;
                bool createInteriorGeometry = !collider.TryGetComponent<Renderer>(out renderer);

                // Set the shadow shape.
                persistantShapeObject.SetShape(vertices, indices, radii, toShadowSpace, ShadowShape2D.WindingOrder.CounterClockwise, true, createInteriorGeometry);

                // Clean up.
                indices.Dispose();
                vertices.Dispose();
                radii.Dispose();
            }
            else
            {
                // No, so clear the shapes.
                ClearShapes(persistantShapeObject);
            }

            // Clean up.
            visibleShapeIndices.Dispose();
        }

        //============================================================================================================
        //                                                  Public
        //============================================================================================================
        public override bool IsShapeSource(in Component sourceComponent) { return sourceComponent is Collider2D; }

        public override void OnPersistantDataCreated(in Component sourceComponent, ShadowShape2D persistantShadowShapeData)
        {
            m_ShadowStateHash = default;
            m_ShadowCombinedShapeMinMaxBounds = default;
            m_LastColliderSpace = Matrix4x4.identity;
        }

        public override void OnBeforeRender(in Component sourceComponent, in Bounds worldCullingBounds, ShadowShape2D persistantShadowShape)
        {
            Collider2D collider = (Collider2D)sourceComponent;
            CalculateShadows(collider, persistantShadowShape, worldCullingBounds);
        }
    }
}
#endif
