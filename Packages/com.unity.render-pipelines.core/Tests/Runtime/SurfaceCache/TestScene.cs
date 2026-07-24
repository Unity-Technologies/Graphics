using System;
using System.Collections.Generic;
using UnityEngine.PathTracing.Core;

namespace UnityEngine.Rendering.Tests
{
    // Builds programmatic geometry, materials, and lights in a SurfaceCacheWorld for Core tests.
    internal sealed class TestScene : IDisposable
    {
        readonly SurfaceCacheWorld m_World;
        readonly List<UnityEngine.Object> m_Resources = new();

        public TestScene(SurfaceCacheWorld world) => m_World = world;

        public void AddDirectionalLight(Vector3 travelDirection, Vector3 linearColor)
        {
            // The world reads the light direction from the transform's forward (column 2).
            var transform = Matrix4x4.identity;
            transform.SetColumn(2, new Vector4(travelDirection.x, travelDirection.y, travelDirection.z, 0f));
            m_World.AddLight(new SurfaceCacheWorld.LightDescriptor
            {
                Type = LightType.Directional,
                LinearLightColor = linearColor,
                Transform = transform,
            });
        }

        public void AddPlane(Vector3 albedo, float halfSize, Vector3 center, bool frontFaceUp = true)
        {
            var materialHandle = m_World.AddMaterial(CreateUniformAlbedoDescriptor(albedo), UVChannel.UV0);
            var mesh = CreateHorizontalQuad(halfSize, frontFaceUp);
            m_Resources.Add(mesh);
            m_World.AddInstance(mesh, new[] { materialHandle }, new[] { 0xFFFFFFFFu }, Matrix4x4.Translate(center));
        }

        public void AddEmissiveQuad(Vector3 emission, float halfSize, Vector3 center, bool frontFaceUp = true)
        {
            var materialHandle = m_World.AddMaterial(new MaterialPool.MaterialDescriptor
            {
                Albedo = Texture2D.blackTexture,
                AlbedoScale = Vector2.one,
                AlbedoOffset = Vector2.zero,
                Alpha = 1f,
                PointSampleAlbedo = true,
                EmissionType = PathTracing.Core.MaterialPropertyType.Color,
                EmissionColor = emission,
                PointSampleEmission = true,
            }, UVChannel.UV0);
            var mesh = CreateHorizontalQuad(halfSize, frontFaceUp);
            m_Resources.Add(mesh);
            m_World.AddInstance(mesh, new[] { materialHandle }, new[] { 0xFFFFFFFFu }, Matrix4x4.Translate(center));
        }

        MaterialPool.MaterialDescriptor CreateUniformAlbedoDescriptor(Vector3 albedo)
        {
            const int size = 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBAFloat, mipChain: false, linear: true)
            {
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = new Color(albedo.x, albedo.y, albedo.z, 1f);
            texture.SetPixels(pixels);
            texture.Apply();
            m_Resources.Add(texture);

            // EmissionType, TransmissionChannels, and emission color default to None/zero.
            return new MaterialPool.MaterialDescriptor
            {
                Albedo = texture,
                AlbedoScale = Vector2.one,
                AlbedoOffset = Vector2.zero,
                Alpha = 1f,
                PointSampleAlbedo = true,
            };
        }

        static Mesh CreateHorizontalQuad(float halfSize, bool frontFaceUp)
        {
            var mesh = new Mesh { hideFlags = HideFlags.DontSave };
            mesh.vertices = new[]
            {
                new Vector3(-halfSize, 0f, -halfSize),
                new Vector3( halfSize, 0f, -halfSize),
                new Vector3(-halfSize, 0f,  halfSize),
                new Vector3( halfSize, 0f,  halfSize),
            };
            var faceNormal = frontFaceUp ? Vector3.up : Vector3.down;
            mesh.normals = new[] { faceNormal, faceNormal, faceNormal, faceNormal };
            // All UVs at the texture center: the albedo is uniform, and this avoids atlas-edge bleed.
            var center = new Vector2(0.5f, 0.5f);
            mesh.uv = new[] { center, center, center, center };
            mesh.uv2 = mesh.uv;
            // Winding sets the geometric face normal: {0,2,1,2,3,1} faces +Y, the reverse faces -Y.
            mesh.triangles = frontFaceUp ? new[] { 0, 2, 1, 2, 3, 1 } : new[] { 0, 1, 2, 2, 1, 3 };
            return mesh;
        }

        public void Dispose()
        {
            foreach (var resource in m_Resources)
                CoreUtils.Destroy(resource);
            m_Resources.Clear();
        }
    }
}
