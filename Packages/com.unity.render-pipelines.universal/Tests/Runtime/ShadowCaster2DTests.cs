#if U2D_ANIMATION_INSTALLED
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.TestTools;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace UnityEngine.Rendering.Universal.Tests
{
    class ShadowCaster2DTests
    {
        GameObject m_Obj;
        Texture2D m_Texture;
        Sprite m_Sprite;

        [SetUp]
        public void Setup()
        {
            m_Obj = new GameObject();
            m_Obj.AddComponent<SpriteRenderer>();
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(m_Obj);
            if (m_Sprite != null)
                Object.DestroyImmediate(m_Sprite);
            if (m_Texture != null)
                Object.DestroyImmediate(m_Texture);
        }

        [Test]
        public void AddShadowCaster2DWithSpriteSkin()
        {
            m_Obj.AddComponent<SpriteSkin>();
            ShadowCaster2D shadowCaster2D = m_Obj.AddComponent<ShadowCaster2D>();

// ShadowCaster2D.shadowShape2DProvider is always null on Standalone.
#if UNITY_EDITOR
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.TypeOf(typeof(ShadowShape2DProvider_SpriteSkin)));
#else
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.Null);
#endif
        }

        [Test]
        public void AddShadowCaster2DWithSpriteSkinWhenInactive()
        {
            m_Obj.AddComponent<SpriteSkin>();
            m_Obj.SetActive(false);
            ShadowCaster2D shadowCaster2D = m_Obj.AddComponent<ShadowCaster2D>();
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.Null);

            m_Obj.SetActive(true);
// ShadowCaster2D.shadowShape2DProvider is always null on Standalone.
#if UNITY_EDITOR
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.TypeOf(typeof(ShadowShape2DProvider_SpriteSkin)));
#else
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.Null);
#endif
        }

// ShadowCaster2D.shadowShape2DProvider is always null on Standalone.
#if UNITY_EDITOR
        // A quad skinned to a single bone, built at runtime so the test needs no asset.
        Sprite CreateSkinnedQuadSprite()
        {
            m_Texture = new Texture2D(4, 4);
            m_Sprite = Sprite.Create(m_Texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4, 0, SpriteMeshType.FullRect);

            m_Sprite.SetVertexCount(4);
            using (var positions = new NativeArray<Vector3>(4, Allocator.Temp))
            using (var weights = new NativeArray<BoneWeight>(4, Allocator.Temp))
            using (var indices = new NativeArray<ushort>(new ushort[] { 0, 1, 2, 2, 3, 0 }, Allocator.Temp))
            using (var bindPoses = new NativeArray<Matrix4x4>(1, Allocator.Temp))
            {
                var p = positions;
                p[0] = new Vector3(-0.5f, -0.5f, 0);
                p[1] = new Vector3(-0.5f, 0.5f, 0);
                p[2] = new Vector3(0.5f, 0.5f, 0);
                p[3] = new Vector3(0.5f, -0.5f, 0);

                var w = weights;
                for (int i = 0; i < w.Length; ++i)
                    w[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1 };

                var b = bindPoses;
                b[0] = Matrix4x4.identity;

                m_Sprite.SetVertexAttribute(VertexAttribute.Position, positions);
                m_Sprite.SetVertexAttribute(VertexAttribute.BlendWeight, weights);
                m_Sprite.SetIndices(indices);
                m_Sprite.SetBindPoses(bindPoses);
            }

            m_Sprite.SetBones(new[]
            {
                new SpriteBone { name = "root", position = Vector3.zero, rotation = Quaternion.identity, length = 1, parentId = -1 }
            });

            return m_Sprite;
        }

        static void CallOnBeforeRender(ShadowCaster2D shadowCaster2D)
        {
            ShapeProviderUtility.CallOnBeforeRender(shadowCaster2D.shadowShape2DProvider, shadowCaster2D.shadowShape2DComponent,
                shadowCaster2D.m_ShadowMesh, new Bounds(Vector3.zero, Vector3.one * 100));
        }

        // UUM-158409: a SpriteSkin whose bones do not move is not deformed again, so the shadow has to
        // pick up the SpriteRenderer flip without waiting for a new deformation.
        [UnityTest]
        public IEnumerator ShadowCaster2DWithSpriteSkin_FollowsFlipWhenBonesAreStill([Values(false, true)] bool forceCpuDeformation)
        {
            SpriteRenderer spriteRenderer = m_Obj.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateSkinnedQuadSprite();

            var bone = new GameObject("root");
            bone.transform.SetParent(m_Obj.transform, false);

            SpriteSkin spriteSkin = m_Obj.AddComponent<SpriteSkin>();
            spriteSkin.forceCpuDeformation = forceCpuDeformation;
            spriteSkin.SetRootBone(bone.transform);
            SpriteSkinState state = spriteSkin.SetBoneTransforms(new[] { bone.transform });
            Assert.That(state, Is.EqualTo(SpriteSkinState.Ready), "Test setup: the SpriteSkin must be valid for deformation.");

            ShadowCaster2D shadowCaster2D = m_Obj.AddComponent<ShadowCaster2D>();
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.TypeOf(typeof(ShadowShape2DProvider_SpriteSkin)));

            // Let the first deformation land and the bones settle, so later frames do not deform again.
            for (int i = 0; i < 5; ++i)
            {
                yield return null;
                CallOnBeforeRender(shadowCaster2D);
            }
            Assert.That(spriteSkin.HasCurrentDeformedVertices(), Is.True, "Test setup: the SpriteSkin must have been deformed.");

            shadowCaster2D.m_ShadowMesh.GetFlip(out bool flipX, out bool flipY);
            Assert.That(flipX, Is.False);
            Assert.That(flipY, Is.False);

            spriteRenderer.flipX = true;
            yield return null;
            CallOnBeforeRender(shadowCaster2D);

            shadowCaster2D.m_ShadowMesh.GetFlip(out flipX, out flipY);
            Assert.That(flipX, Is.True, "The shadow did not follow SpriteRenderer.flipX.");
            Assert.That(flipY, Is.False);

            spriteRenderer.flipX = false;
            spriteRenderer.flipY = true;
            yield return null;
            CallOnBeforeRender(shadowCaster2D);

            shadowCaster2D.m_ShadowMesh.GetFlip(out flipX, out flipY);
            Assert.That(flipX, Is.False, "The shadow did not follow SpriteRenderer.flipX.");
            Assert.That(flipY, Is.True, "The shadow did not follow SpriteRenderer.flipY.");
        }
#endif
    }
}
#endif
