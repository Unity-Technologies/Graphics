#if SURFACE_CACHE && (SURFACE_CACHE_SUPPORTED || UNITY_EDITOR)

using NUnit.Framework;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.Rendering.Universal.Tests
{
    class SurfaceCacheWorldAdapterTests
    {
        [Test]
        public void Update_AddsThenRemovesLight_AsReportedByTheChangeSet()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = new GameObject("SurfaceCacheWorldAdapterTest Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.white;
                light.intensity = 1f;
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
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
                env.Dispose();
            }
        }

        [Test]
        public void Update_PropagatesDirectionalLightIntensityChange_AsReportedByTheChangeSet()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = new GameObject("SurfaceCacheWorldAdapterTest Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1f;

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
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
                env.Dispose();
            }
        }

        [Test]
        public void Update_PropagatesPunctualLightIntensityChange_AsReportedByTheChangeSet()
        {
            var env = CreateEnvironmentOrIgnore();
            GameObject lightGo = null;
            try
            {
                lightGo = new GameObject("SurfaceCacheWorldAdapterTest Point Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.white;
                light.intensity = 1f;

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
                if (lightGo != null)
                    Object.DestroyImmediate(lightGo);
                env.Dispose();
            }
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
                Assert.Ignore("The Compute ray-tracing backend is not supported on this platform.");

            var rtResources = new RayTracingResources();
            if (!rtResources.LoadFromRenderPipelineResources())
                Assert.Ignore("Ray tracing resources are unavailable (requires an active SRP with the resources registered).");

            var worldResources = new WorldResourceSet();
            if (!worldResources.LoadFromRenderPipelineResources())
                Assert.Ignore("Surface cache world resources are unavailable (requires an active SRP with the resources registered).");

            var resourceSet = GraphicsSettings.GetRenderPipelineSettings<SurfaceCacheRenderPipelineResourceSet>();
            if (resourceSet == null || resourceSet.fallbackMaterial == null)
                Assert.Ignore("Surface cache render pipeline resources are unavailable.");

            var ctx = new RayTracingContext(RayTracingBackend.Compute, rtResources);
            var world = new SurfaceCacheWorld();
            world.Init(ctx, worldResources);
            var adapter = new SurfaceCacheWorldAdapter(resourceSet.fallbackMaterial);
            return new Environment(ctx, world, adapter);
        }

        static void Update(SurfaceCacheWorldAdapter adapter, SurfaceCacheWorldChangeSet changes, SurfaceCacheWorld world)
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
                0xFFFFFFFFu,
                world);
        }
    }
}

#endif
