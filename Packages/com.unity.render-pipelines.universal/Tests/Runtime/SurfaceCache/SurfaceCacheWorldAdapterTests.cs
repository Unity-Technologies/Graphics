#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using NUnit.Framework;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.Rendering.Universal.Tests
{
    class SurfaceCacheWorldAdapterTests
    {
        [Test]
        public void UpdateLight_ChangedThenDestroyed_LightIsAddedThenRemoved()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = CreateLightGameObject(out var light);
                var lightId = light.GetEntityId();

                var added = new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } };
                Update(env.Adapter, added, env.World);
                Assert.AreEqual(1u, env.World.GetPunctualLightCount(), "Light should be present after it is reported as changed.");

                Object.DestroyImmediate(lightGo);
                lightGo = null;
                var removed = new SurfaceCacheWorldChangeSet { LightDestroyedList = new[] { lightId } };
                Update(env.Adapter, removed, env.World);
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "Light should be gone after it is reported as destroyed.");
            }
            finally
            {
                env.Dispose();
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
            }
        }

        [Test]
        public void UpdateLight_DirectionalIntensityChanged_IntensityIsPropagated()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = CreateLightGameObject(out var light);
                light.type = LightType.Directional;

                var added = new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } };
                Update(env.Adapter, added, env.World);

                var initial = env.World.GetDirectionalLight();
                Assert.IsTrue(initial.HasValue, "Directional light should be present after it is reported as changed.");
                Assert.AreEqual(Util.GetLinearLightColor(light, light.bounceIntensity), initial.Value.Intensity, "Stored intensity should match the reported light.");

                light.intensity = 4f;
                var changed = new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } };
                Update(env.Adapter, changed, env.World);

                var updated = env.World.GetDirectionalLight();
                Assert.IsTrue(updated.HasValue, "Directional light should still be present after the intensity change.");
                Assert.AreEqual(Util.GetLinearLightColor(light, light.bounceIntensity), updated.Value.Intensity, "Stored intensity should reflect the changed light intensity.");
                Assert.AreNotEqual(initial.Value.Intensity, updated.Value.Intensity, "The intensity change should have propagated to the world.");
            }
            finally
            {
                env.Dispose();
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
            }
        }

        [Test]
        public void UpdateLight_PunctualIntensityChanged_IntensityIsPropagated()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = CreateLightGameObject(out var light);

                var added = new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } };
                Update(env.Adapter, added, env.World);

                Assert.AreEqual(1u, env.World.GetPunctualLightCount(), "Point light should be present after it is reported as changed.");
                var initial = env.World.GetPunctualLight(0).Intensity;
                Assert.AreEqual(Util.GetLinearLightColor(light, light.bounceIntensity), initial, "Stored intensity should match the reported light.");

                light.intensity = 4f;
                var changed = new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } };
                Update(env.Adapter, changed, env.World);

                Assert.AreEqual(1u, env.World.GetPunctualLightCount(), "The reported light should be updated in place, not duplicated.");
                var updated = env.World.GetPunctualLight(0).Intensity;
                Assert.AreEqual(Util.GetLinearLightColor(light, light.bounceIntensity), updated, "Stored intensity should reflect the changed light intensity.");
                Assert.AreNotEqual(initial, updated, "The intensity change should have propagated to the world.");
            }
            finally
            {
                env.Dispose();
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
            }
        }

        [Test]
        public void UpdateMeshRenderer_ChangedThenDestroyed_RendererIsAddedThenRemoved()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject go = null;
            try
            {
                go = CreateMeshRendererGameObject(mesh, out var renderer);
                var rendererId = renderer.GetEntityId();

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Renderer should be present after it is reported as changed.");
                Assert.AreEqual(1, env.World.GetMaterialCount(), "The fallback material should be acquired for the renderer.");

                Object.DestroyImmediate(go);
                go = null;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererDestroyedList = new[] { rendererId } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Renderer should be gone after it is reported as destroyed.");
                Assert.AreEqual(0, env.World.GetMaterialCount(), "The fallback material should be released with the renderer.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateMeshRenderer_ChangedWhileDisabled_RendererIsNotAdded()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject go = null;
            try
            {
                go = CreateMeshRendererGameObject(mesh, out var renderer);
                renderer.enabled = false;

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "A disabled renderer should not be added to the world.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateMeshRenderer_ChangedWithoutMesh_RendererIsNotAdded()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject go = null;
            try
            {
                go = CreateMeshRendererGameObject(null, out var renderer);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "A renderer without a mesh should not be added to the world.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void UpdateMeshRenderer_ChangedWithoutMeshFilter_RendererIsNotAdded()
        {
            // A MeshRenderer without a MeshFilter (e.g. a TextMesh object) must be skipped, not throw. In the
            // editor a missing component is a placeholder object that throws when dereferenced.
            var env = CreateEnvironmentOrIgnore();
            GameObject go = null;
            try
            {
                go = new GameObject("SurfaceCacheWorldAdapterTest RendererWithoutMeshFilter");
                var renderer = go.AddComponent<MeshRenderer>();

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "A renderer without a MeshFilter should not be added to the world.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void UpdateMeshRenderer_DisabledAfterAdd_RendererIsRemoved()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject go = null;
            try
            {
                go = CreateMeshRendererGameObject(mesh, out var renderer);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Renderer should be present after it is reported as changed.");

                renderer.enabled = false;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Renderer should be removed once it is reported as changed while disabled.");
                Assert.AreEqual(0, env.World.GetMaterialCount(), "The fallback material should be released with the renderer.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateMeshRenderer_ChangedTwice_RendererIsNotDuplicated()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject go = null;
            try
            {
                go = CreateMeshRendererGameObject(mesh, out var renderer);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World);
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "A repeated change report should update the renderer in place, not duplicate it.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateMeshRenderer_UnknownTransformChanged_RendererIsAdded()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject go = null;
            try
            {
                go = CreateMeshRendererGameObject(mesh, out var renderer);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererTransformChangedList = new Component[] { renderer } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "A transform-change report for an unknown renderer should add it to the world.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererTransformChangedList = new Component[] { renderer } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "A transform-change report for an in-world renderer should update it in place, not duplicate it.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Update_TransformChangedAndDestroyedInSameChangeSet_ObjectsAreRemoved()
        {
            // A change set can report a transform change and a destruction for the same object (the object moved
            // and was destroyed within the same frame). The destruction must win no matter which category the
            // adapter processes first: the object must end up removed, not kept alive or re-added by the
            // transform report. The components are deliberately kept alive: only a live transform report can
            // (re-)add the object.
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject rendererGo = null;
            GameObject lightGo = null;
            try
            {
                rendererGo = CreateMeshRendererGameObject(mesh, out var renderer);
                lightGo = CreateLightGameObject(out var light);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet
                {
                    MeshRendererChangedList = new Object[] { renderer },
                    LightChangedList = new Object[] { light },
                }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount());
                Assert.AreEqual(1u, env.World.GetPunctualLightCount());

                var combined = new SurfaceCacheWorldChangeSet
                {
                    MeshRendererTransformChangedList = new Component[] { renderer },
                    MeshRendererDestroyedList = new[] { renderer.GetEntityId() },
                    LightTransformChangedList = new Component[] { light },
                    LightDestroyedList = new[] { light.GetEntityId() },
                };
                Update(env.Adapter, combined, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "An in-world renderer reported as both transform-changed and destroyed should be removed.");
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "An in-world light reported as both transform-changed and destroyed should be removed.");

                // The objects are now unknown to the adapter, so the transform report alone would add them;
                // combined with a destroyed report they must still end up removed.
                Update(env.Adapter, combined, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "A renderer reported as both transform-changed and destroyed should not be (re-)added.");
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "A light reported as both transform-changed and destroyed should not be (re-)added.");
            }
            finally
            {
                env.Dispose();
                if (rendererGo != null)
                    Object.DestroyImmediate(rendererGo);
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Update_DestroyedReportsForUnknownIds_WorldIsUnchanged()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject rendererGo = null;
            GameObject lightGo = null;
            GameObject unreportedGo = null;
            try
            {
                rendererGo = CreateMeshRendererGameObject(mesh, out var renderer);
                lightGo = CreateLightGameObject(out var light);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet
                {
                    MeshRendererChangedList = new Object[] { renderer },
                    LightChangedList = new Object[] { light },
                }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount());
                Assert.AreEqual(1u, env.World.GetPunctualLightCount());

                unreportedGo = new GameObject("SurfaceCacheWorldAdapterTest Unreported");
                var unreportedId = unreportedGo.AddComponent<Light>().GetEntityId();

                Update(env.Adapter, new SurfaceCacheWorldChangeSet
                {
                    MeshRendererDestroyedList = new[] { unreportedId, EntityId.None },
                    LightDestroyedList = new[] { unreportedId, EntityId.None },
                }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "A destroyed report for an id that was never added should not affect renderers.");
                Assert.AreEqual(1u, env.World.GetPunctualLightCount(), "A destroyed report for an id that was never added should not affect lights.");
            }
            finally
            {
                env.Dispose();
                if (rendererGo != null)
                    Object.DestroyImmediate(rendererGo);
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
                if (unreportedGo != null)
                    Object.DestroyImmediate(unreportedGo);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateMeshRenderer_DestroyedWhileFilteredOut_WorldIsUnchanged()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject go = null;
            try
            {
                go = CreateMeshRendererGameObject(mesh, out var renderer);
                renderer.renderingLayerMask = 2;
                var rendererId = renderer.GetEntityId();

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "A renderer excluded by the filter should not be in the world.");

                Object.DestroyImmediate(go);
                go = null;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererDestroyedList = new[] { rendererId } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Destroying a registered but filtered-out renderer should be handled cleanly.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 2u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "A destroyed renderer should not resurface when the filter changes to match its mask.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateMeshRenderer_RenderingLayerMaskFilterChanged_InclusionFollowsFilter()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject go = null;
            try
            {
                go = CreateMeshRendererGameObject(mesh, out var renderer);
                renderer.renderingLayerMask = 2;

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Renderer should be excluded while the filter does not match its mask.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 2u);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Renderer should be added when the filter changes to match its mask.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Renderer should be removed when the filter changes to exclude its mask again.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateMeshRenderer_RenderingLayerMaskChanged_InclusionFollowsMask()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject go = null;
            try
            {
                go = CreateMeshRendererGameObject(mesh, out var renderer);
                renderer.renderingLayerMask = 1;

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World, 1u);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Renderer should be present while its mask matches the filter.");

                renderer.renderingLayerMask = 2;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Renderer should be removed when its mask changes to one excluded by the filter.");

                renderer.renderingLayerMask = 1;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererChangedList = new Object[] { renderer } }, env.World, 1u);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Renderer should be re-added when its mask changes back to match the filter.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateLight_RenderingLayerMaskFilterChanged_InclusionFollowsFilter()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = CreateLightGameObject(out var light);
                light.renderingLayerMask = 2;

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } }, env.World, 1u);
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "Light should be excluded while the filter does not match its mask.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 2u);
                Assert.AreEqual(1u, env.World.GetPunctualLightCount(), "Light should be added when the filter changes to match its mask.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 1u);
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "Light should be removed when the filter changes to exclude its mask again.");
            }
            finally
            {
                env.Dispose();
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
            }
        }

        [Test]
        public void UpdateLight_RenderingLayerMaskChanged_InclusionFollowsMask()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = CreateLightGameObject(out var light);
                light.renderingLayerMask = 1;

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } }, env.World, 1u);
                Assert.AreEqual(1u, env.World.GetPunctualLightCount(), "Light should be present while its mask matches the filter.");

                light.renderingLayerMask = 2;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } }, env.World, 1u);
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "Light should be removed when its mask changes to one excluded by the filter.");

                light.renderingLayerMask = 1;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } }, env.World, 1u);
                Assert.AreEqual(1u, env.World.GetPunctualLightCount(), "Light should be re-added when its mask changes back to match the filter.");
            }
            finally
            {
                env.Dispose();
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
            }
        }

        [Test]
        public void UpdateLight_ChangedWhileDisabled_LightIsNotAdded()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = CreateLightGameObject(out var light);
                light.enabled = false;

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } }, env.World);
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "A disabled light should not be added to the world.");
            }
            finally
            {
                env.Dispose();
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
            }
        }

        [Test]
        public void UpdateLight_DestroyedWhileFilteredOut_WorldIsUnchanged()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = CreateLightGameObject(out var light);
                light.renderingLayerMask = 2;
                var lightId = light.GetEntityId();

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } }, env.World, 1u);
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "A light excluded by the filter should not be in the world.");

                Object.DestroyImmediate(lightGo);
                lightGo = null;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightDestroyedList = new[] { lightId } }, env.World, 1u);
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "Destroying a registered but filtered-out light should be handled cleanly.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 2u);
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "A destroyed light should not resurface when the filter changes to match its mask.");
            }
            finally
            {
                env.Dispose();
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
            }
        }

        [Test]
        public void UpdateLight_DisabledAfterAdd_LightIsRemoved()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = CreateLightGameObject(out var light);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } }, env.World);
                Assert.AreEqual(1u, env.World.GetPunctualLightCount(), "Light should be present after it is reported as changed.");

                light.enabled = false;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } }, env.World);
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "Light should be removed once it is reported as changed while disabled.");
            }
            finally
            {
                env.Dispose();
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
            }
        }

        [Test]
        public void UpdateLight_ChangedWhileBaked_LightIsNotAdded()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = CreateLightGameObject(out var light);
                light.bakingOutput = new LightBakingOutput { isBaked = true, lightmapBakeType = LightmapBakeType.Baked };

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightChangedList = new Object[] { light } }, env.World);
                Assert.AreEqual(0u, env.World.GetPunctualLightCount(), "A baked light should not be added to the world.");
            }
            finally
            {
                env.Dispose();
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
            }
        }

        [Test]
        public void UpdateLight_UnknownTransformChanged_LightIsAdded()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = CreateLightGameObject(out var light);
                lightGo.transform.position = new Vector3(1f, 2f, 3f);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { LightTransformChangedList = new Component[] { light } }, env.World);
                Assert.AreEqual(1u, env.World.GetPunctualLightCount(), "A transform-change report for an unknown light should add it to the world.");
                Assert.AreEqual(new Vector3(1f, 2f, 3f), env.World.GetPunctualLight(0).Position, "The stored light position should match the reported transform.");
            }
            finally
            {
                env.Dispose();
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
            }
        }

#if ENABLE_TERRAIN_MODULE
        static GameObject CreateTerrainGameObject(out Terrain terrain, out TerrainData terrainData)
        {
            terrainData = new TerrainData
            {
                heightmapResolution = 33,
                size = new Vector3(10f, 5f, 10f),
            };
            var go = Terrain.CreateTerrainGameObject(terrainData);
            go.name = "SurfaceCacheWorldAdapterTest Terrain";
            terrain = go.GetComponent<Terrain>();
            return go;
        }

        [Test]
        public void UpdateTerrain_ChangedThenDestroyed_TerrainIsAddedThenRemoved()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject go = null;
            TerrainData terrainData = null;
            try
            {
                go = CreateTerrainGameObject(out var terrain, out terrainData);
                var terrainId = terrain.GetEntityId();

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainChangedList = new Object[] { terrain } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Terrain should be present after it is reported as changed.");

                Object.DestroyImmediate(go);
                go = null;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainDestroyedList = new[] { terrainId } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Terrain should be gone after it is reported as destroyed.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                if (terrainData != null)
                    Object.DestroyImmediate(terrainData);
            }
        }

        [Test]
        public void UpdateTerrain_RenderingLayerMaskFilterChanged_InclusionFollowsFilter()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject go = null;
            TerrainData terrainData = null;
            try
            {
                go = CreateTerrainGameObject(out var terrain, out terrainData);
                terrain.renderingLayerMask = 2;

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainChangedList = new Object[] { terrain } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Terrain should be excluded while the filter does not match its mask.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 2u);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Terrain should be added when the filter changes to match its mask.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Terrain should be removed when the filter changes to exclude its mask again.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                if (terrainData != null)
                    Object.DestroyImmediate(terrainData);
            }
        }

        [Test]
        public void UpdateTerrain_RenderingLayerMaskChanged_InclusionFollowsMask()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject go = null;
            TerrainData terrainData = null;
            try
            {
                go = CreateTerrainGameObject(out var terrain, out terrainData);
                terrain.renderingLayerMask = 1;

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainChangedList = new Object[] { terrain } }, env.World, 1u);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Terrain should be present while its mask matches the filter.");

                terrain.renderingLayerMask = 2;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainChangedList = new Object[] { terrain } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Terrain should be removed when its mask changes to one excluded by the filter.");

                terrain.renderingLayerMask = 1;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainChangedList = new Object[] { terrain } }, env.World, 1u);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Terrain should be re-added when its mask changes back to match the filter.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                if (terrainData != null)
                    Object.DestroyImmediate(terrainData);
            }
        }

        [Test]
        public void UpdateTerrain_TerrainDataChanged_TerrainIsRebuiltInPlace()
        {
            // A TerrainData change is applied as a remove + re-add of the world instance: immediately in the player,
            // deferred by a fixed number of adapter updates (ticks) in the editor. The instance and material counts
            // must be unchanged afterwards: a skipped remove would leave 2 instances, a skipped re-add would leave 0,
            // and a material leak would grow the material count.
            var env = CreateEnvironmentOrIgnore();
            GameObject go = null;
            TerrainData terrainData = null;
            try
            {
                go = CreateTerrainGameObject(out var terrain, out terrainData);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainChangedList = new Object[] { terrain } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Terrain should be present after it is reported as changed.");
                int materialCount = env.World.GetMaterialCount();

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainDataChangedList = new Object[] { terrainData } }, env.World);
#if UNITY_EDITOR
                Assert.AreEqual(1, env.Adapter.GetDeferredTerrainRebuildCount(), "The TerrainData change should schedule a deferred rebuild.");

                for (int i = 0; i < SurfaceCacheWorldAdapter.k_TerrainRebuildDelayInTicks + 10; i++)
                    Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World);

                Assert.AreEqual(0, env.Adapter.GetDeferredTerrainRebuildCount(), "The scheduled rebuild should have been consumed within the pumped updates.");
#endif
                Assert.AreEqual(1, env.World.GetInstanceCount(), "The rebuild should remove and re-add the terrain instance, leaving exactly one.");
                Assert.AreEqual(materialCount, env.World.GetMaterialCount(), "The rebuild should not leak materials.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                if (terrainData != null)
                    Object.DestroyImmediate(terrainData);
            }
        }

        [Test]
        public void UpdateTerrain_UnknownTransformChanged_TerrainIsAdded()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject go = null;
            TerrainData terrainData = null;
            try
            {
                go = CreateTerrainGameObject(out var terrain, out terrainData);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainTransformChangedList = new Component[] { terrain } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "A transform-change report for an unknown terrain should add it to the world.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainTransformChangedList = new Component[] { terrain } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "A transform-change report for an in-world terrain should update it in place, not duplicate it.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                if (terrainData != null)
                    Object.DestroyImmediate(terrainData);
            }
        }

        [Test]
        public void UpdateTerrain_DisabledAfterAdd_TerrainIsRemoved()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject go = null;
            TerrainData terrainData = null;
            try
            {
                go = CreateTerrainGameObject(out var terrain, out terrainData);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainChangedList = new Object[] { terrain } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Terrain should be present after it is reported as changed.");

                terrain.enabled = false;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainChangedList = new Object[] { terrain } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Terrain should be removed once it is reported as changed while disabled.");
                Assert.AreEqual(0, env.World.GetMaterialCount(), "The fallback material should be released with the terrain.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                if (terrainData != null)
                    Object.DestroyImmediate(terrainData);
            }
        }

        [Test]
        public void UpdateTerrain_ChangedTwice_TerrainIsNotDuplicated()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject go = null;
            TerrainData terrainData = null;
            try
            {
                go = CreateTerrainGameObject(out var terrain, out terrainData);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainChangedList = new Object[] { terrain } }, env.World);
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { TerrainChangedList = new Object[] { terrain } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "A repeated change report should update the terrain in place, not duplicate it.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                if (terrainData != null)
                    Object.DestroyImmediate(terrainData);
            }
        }
#endif

        [Test]
        public void UpdateEntityInstance_ChangedThenDestroyed_InstanceIsAddedThenRemoved()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            try
            {
                var key = CreateEntityKey(1);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, mesh) } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Entity instance should be present after it is reported as changed.");
                Assert.AreEqual(1, env.World.GetMaterialCount(), "The fallback material should be acquired for the entity instance.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceDestroyedList = new[] { key } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Entity instance should be gone after it is reported as destroyed.");
                Assert.AreEqual(0, env.World.GetMaterialCount(), "The fallback material should be released with the entity instance.");
            }
            finally
            {
                env.Dispose();
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateEntityInstance_ChangedWhileInvisible_InstanceIsNotAdded()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            try
            {
                var record = CreateEntityRecord(CreateEntityKey(1), mesh, visible: false);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { record } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "An invisible entity instance should not be added to the world.");
            }
            finally
            {
                env.Dispose();
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateEntityInstance_ChangedWithoutMesh_InstanceIsNotAdded()
        {
            var env = CreateEnvironmentOrIgnore();
            try
            {
                var record = CreateEntityRecord(CreateEntityKey(1), null);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { record } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "An entity instance without a mesh should not be added to the world.");
            }
            finally
            {
                env.Dispose();
            }
        }

        [Test]
        public void UpdateEntityInstance_HiddenAfterAdd_InstanceIsRemoved()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            try
            {
                var key = CreateEntityKey(1);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, mesh) } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Entity instance should be present after it is reported as changed.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, mesh, visible: false) } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Entity instance should be removed once a record reports it as invisible.");
                Assert.AreEqual(0, env.World.GetMaterialCount(), "The fallback material should be released with the entity instance.");
            }
            finally
            {
                env.Dispose();
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateEntityInstance_ChangedTwice_InstanceIsNotDuplicated()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            try
            {
                var key = CreateEntityKey(1);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, mesh) } }, env.World);
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, mesh) } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "A repeated change report should update the entity instance in place, not duplicate it.");
                Assert.AreEqual(1, env.World.GetMaterialCount(), "A repeated change report should not acquire additional materials.");
            }
            finally
            {
                env.Dispose();
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateEntityInstance_MeshChanged_InstanceIsRebuiltInPlace()
        {
            var env = CreateEnvironmentOrIgnore();
            var meshA = CreateQuadMesh();
            var meshB = CreateQuadMesh();
            try
            {
                var key = CreateEntityKey(1);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, meshA) } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Entity instance should be present after it is reported as changed.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, meshB) } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "A mesh change should rebuild the entity instance in place, leaving exactly one.");
                Assert.AreEqual(1, env.World.GetMaterialCount(), "A mesh change should not leak materials.");
            }
            finally
            {
                env.Dispose();
                Object.DestroyImmediate(meshA);
                Object.DestroyImmediate(meshB);
            }
        }

        [Test]
        public void UpdateEntityInstance_TransformChangedForUnknownKey_WorldIsUnchanged()
        {
            // A transform record carries no mesh or material data and so cannot add an unknown instance.
            var env = CreateEnvironmentOrIgnore();
            try
            {
                var transformRecord = new SurfaceCacheEntityTransformRecord
                {
                    Key = CreateEntityKey(1),
                    LocalToWorld = Matrix4x4.Translate(new Vector3(1f, 2f, 3f)),
                };

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceTransformChangedList = new[] { transformRecord } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "A transform record for an unknown entity should not add anything to the world.");
            }
            finally
            {
                env.Dispose();
            }
        }

        [Test]
        public void UpdateEntityInstance_TransformChangedAndDestroyedInSameChangeSet_InstanceIsRemoved()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            try
            {
                var key = CreateEntityKey(1);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, mesh) } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount());

                var combined = new SurfaceCacheWorldChangeSet
                {
                    EntityInstanceTransformChangedList = new[] { new SurfaceCacheEntityTransformRecord { Key = key, LocalToWorld = Matrix4x4.identity } },
                    EntityInstanceDestroyedList = new[] { key },
                };
                Update(env.Adapter, combined, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "An in-world entity instance reported as both transform-changed and destroyed should be removed.");
            }
            finally
            {
                env.Dispose();
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateEntityInstance_RenderingLayerMaskFilterChanged_InclusionFollowsFilter()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            try
            {
                var record = CreateEntityRecord(CreateEntityKey(1), mesh, renderingLayerMask: 2u);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { record } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Entity instance should be excluded while the filter does not match its mask.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 2u);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Entity instance should be added when the filter changes to match its mask.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Entity instance should be removed when the filter changes to exclude its mask again.");
            }
            finally
            {
                env.Dispose();
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateEntityInstance_DestroyedWhileFilteredOut_WorldIsUnchanged()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            try
            {
                var key = CreateEntityKey(1);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, mesh, renderingLayerMask: 2u) } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "An entity instance excluded by the filter should not be in the world.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceDestroyedList = new[] { key } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "Destroying a registered but filtered-out entity instance should be handled cleanly.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 2u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "A destroyed entity instance should not resurface when the filter changes to match its mask.");
            }
            finally
            {
                env.Dispose();
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateEntityInstance_TransformChangedWhileInWorld_WorldTransformIsUpdated()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            try
            {
                var key = CreateEntityKey(1);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, mesh) } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount());

                var movedTransform = Matrix4x4.Translate(new Vector3(4f, 5f, 6f));
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceTransformChangedList = new[] { new SurfaceCacheEntityTransformRecord { Key = key, LocalToWorld = movedTransform } } }, env.World);

                Assert.AreEqual(1, env.World.GetInstanceCount(), "A transform change should update the instance in place, not duplicate it.");
#if UNITY_EDITOR
                Assert.IsTrue(env.Adapter.TryGetEntityInstanceAppliedLocalToWorld(key, out var appliedTransform));
                Assert.AreEqual(movedTransform, appliedTransform, "A transform record for an in-world instance should be applied to the world.");
#endif
            }
            finally
            {
                env.Dispose();
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateEntityInstance_TransformChangedWhileFilteredOut_FilterFlipAddsAtNewTransform()
        {
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            try
            {
                var key = CreateEntityKey(1);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceChangedList = new[] { CreateEntityRecord(key, mesh, renderingLayerMask: 2u) } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "An entity instance excluded by the filter should not be in the world.");

                var movedTransform = Matrix4x4.Translate(new Vector3(1f, 2f, 3f));
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceTransformChangedList = new[] { new SurfaceCacheEntityTransformRecord { Key = key, LocalToWorld = movedTransform } } }, env.World, 1u);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "A transform change should not add a filtered-out instance to the world.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet(), env.World, 2u);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "The instance should be added when the filter changes to match its mask.");
#if UNITY_EDITOR
                Assert.IsTrue(env.Adapter.TryGetEntityInstanceAppliedLocalToWorld(key, out var appliedTransform));
                Assert.AreEqual(movedTransform, appliedTransform, "The instance should enter the world at the transform received while it was filtered out, not the stale one it was registered with.");
#endif
            }
            finally
            {
                env.Dispose();
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void UpdateEntityInstance_SharedMaterialWithMeshRenderer_MaterialIsRefCountedAcrossSets()
        {
            // Both instances take the fallback path, so they must share a single refcounted material entry.
            var env = CreateEnvironmentOrIgnore();
            var mesh = CreateQuadMesh();
            GameObject go = null;
            try
            {
                go = CreateMeshRendererGameObject(mesh, out var renderer);
                var rendererId = renderer.GetEntityId();
                var key = CreateEntityKey(1);

                Update(env.Adapter, new SurfaceCacheWorldChangeSet
                {
                    MeshRendererChangedList = new Object[] { renderer },
                    EntityInstanceChangedList = new[] { CreateEntityRecord(key, mesh) },
                }, env.World);
                Assert.AreEqual(2, env.World.GetInstanceCount(), "The renderer and the entity instance should both be in the world.");
                Assert.AreEqual(1, env.World.GetMaterialCount(), "Both instances should share a single fallback material entry.");

                Update(env.Adapter, new SurfaceCacheWorldChangeSet { EntityInstanceDestroyedList = new[] { key } }, env.World);
                Assert.AreEqual(1, env.World.GetInstanceCount(), "Only the entity instance should be removed.");
                Assert.AreEqual(1, env.World.GetMaterialCount(), "The shared material should stay while the renderer still references it.");

                Object.DestroyImmediate(go);
                go = null;
                Update(env.Adapter, new SurfaceCacheWorldChangeSet { MeshRendererDestroyedList = new[] { rendererId } }, env.World);
                Assert.AreEqual(0, env.World.GetInstanceCount(), "The renderer should be gone after it is reported as destroyed.");
                Assert.AreEqual(0, env.World.GetMaterialCount(), "The shared material should be released with its last reference.");
            }
            finally
            {
                env.Dispose();
                if (go != null)
                    Object.DestroyImmediate(go);
                Object.DestroyImmediate(mesh);
            }
        }

        static EntityId CreateEntityKey(int index)
        {
            // Version 1 in the [Version:24 | TypeId:12 | Index:28] layout, so index 0 is distinct from EntityId.None.
            return EntityId.FromULong((1ul << 40) | (uint)index);
        }

        // No materials are assigned on purpose: null materials take the silent fallback path in the adapter.
        static SurfaceCacheEntityInstanceRecord CreateEntityRecord(EntityId key, Mesh mesh, uint renderingLayerMask = 1u, bool visible = true)
        {
            return new SurfaceCacheEntityInstanceRecord
            {
                Key = key,
                Mesh = mesh,
                Materials = mesh != null ? new Material[mesh.subMeshCount] : null,
                LocalToWorld = Matrix4x4.identity,
                RenderingLayerMask = renderingLayerMask,
                Visible = visible,
            };
        }

        readonly struct Environment
        {
            public readonly RayTracingContext Context;
            public readonly SurfaceCacheWorld World;
            public readonly SurfaceCacheWorldAdapter Adapter;

            public Environment(RayTracingContext context, SurfaceCacheWorld world, SurfaceCacheWorldAdapter adapter)
            {
                Context = context;
                World = world;
                Adapter = adapter;
            }

            public void Dispose()
            {
                Adapter?.CleanUp(World);
                World?.Dispose();
                Context?.Dispose();
            }
        }

        static Environment CreateEnvironmentOrIgnore()
        {
            if (!RayTracingContext.IsBackendSupported(RayTracingBackend.Compute))
                Assert.Ignore("Requires the Compute ray-tracing backend.");
            if (Application.platform == RuntimePlatform.PS5)
                Assert.Ignore("SurfaceCache adapter tests do not support the Compute ray-tracing backend on this platform.");

            var rtResources = new RayTracingResources();
            if (!rtResources.LoadFromRenderPipelineResources())
                Assert.Ignore("Ray tracing resources are unavailable (requires an active SRP with the resources registered).");

            var worldResources = new WorldResourceSet();
            if (!worldResources.LoadFromRenderPipelineResources())
                Assert.Ignore("Surface cache world resources are unavailable (requires an active SRP with the resources registered).");

            var resourceSet = GraphicsSettings.GetRenderPipelineSettings<SurfaceCacheRenderPipelineResourceSet>();
            if (resourceSet == null || resourceSet.fallbackMaterial == null)
                Assert.Ignore("Surface cache render pipeline resources are unavailable.");

            var coreResourceSet = GraphicsSettings.GetRenderPipelineSettings<Rendering.SurfaceCacheRenderPipelineResourceSet>();
            if (coreResourceSet == null)
                Assert.Ignore("Surface cache core render pipeline resources are unavailable.");

            if (coreResourceSet.emissiveTriangleAdditionComputeShader == null
                || !coreResourceSet.emissiveTriangleAdditionComputeShader.HasKernel("Add")
                || coreResourceSet.emissiveTriangleRemovalComputeShader == null
                || !coreResourceSet.emissiveTriangleRemovalComputeShader.HasKernel("Remove"))
                Assert.Ignore("Surface cache emissive triangle compute kernels are unavailable on this graphics API.");

            var ctx = new RayTracingContext(RayTracingBackend.Compute, rtResources);
            var world = new SurfaceCacheWorld();
            world.Init(ctx, worldResources, coreResourceSet.emissiveTriangleAdditionComputeShader, coreResourceSet.emissiveTriangleRemovalComputeShader);
            var adapter = new SurfaceCacheWorldAdapter(resourceSet.fallbackMaterial);
            return new Environment(ctx, world, adapter);
        }

        static void Update(SurfaceCacheWorldAdapter adapter, SurfaceCacheWorldChangeSet changes, SurfaceCacheWorld world, uint renderingLayerMaskFilter = 0xFFFFFFFFu)
        {
            adapter.Update(
                changes,
                AmbientMode.Flat,
                null,
                Color.black,
                Color.black,
                Color.black,
                1f,
                false,
                renderingLayerMaskFilter,
                world);
        }

        static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh { name = "SurfaceCacheWorldAdapterTest Quad" };
            mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            return mesh;
        }

        // No material is assigned on purpose: a null material takes the silent fallback path in the adapter.
        static GameObject CreateMeshRendererGameObject(Mesh mesh, out MeshRenderer renderer)
        {
            var go = new GameObject("SurfaceCacheWorldAdapterTest Renderer");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = go.AddComponent<MeshRenderer>();
            return go;
        }

        static GameObject CreateLightGameObject(out Light light)
        {
            var go = new GameObject("SurfaceCacheWorldAdapterTest Light");
            light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 1f;
            return go;
        }
    }
}

#endif
