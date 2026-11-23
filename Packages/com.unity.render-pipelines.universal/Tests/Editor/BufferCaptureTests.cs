using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.Universal.Tests
{
    class BufferCaptureParseTargetsTests
    {
        static string[] Names(List<BufferCapture.RequestedTarget> targets) => targets.Select(t => t.target.name).ToArray();

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void ParseTargets_NoBuffers_DefaultsToNamedColor(string buffers)
        {
            var targets = BufferCapture.ParseTargets(buffers);

            Assert.That(Names(targets), Is.EqualTo(new[] { "color" }));
            Assert.IsTrue(targets[0].named);
        }

        [Test]
        public void ParseTargets_TrimsAndIgnoresCase()
        {
            var targets = BufferCapture.ParseTargets(" Depth , ALBEDO ");

            Assert.That(Names(targets), Is.EqualTo(new[] { "depth", "albedo" }));
            Assert.IsTrue(targets.All(t => t.named));
        }

        [Test]
        public void ParseTargets_IgnoresEmptyEntries()
        {
            var targets = BufferCapture.ParseTargets("color,,depth,");

            Assert.That(Names(targets), Is.EqualTo(new[] { "color", "depth" }));
        }

        [Test]
        public void ParseTargets_RemovesDuplicates()
        {
            var targets = BufferCapture.ParseTargets("depth,color,depth");

            Assert.That(Names(targets), Is.EqualTo(new[] { "depth", "color" }));
        }

        [Test]
        public void ParseTargets_All_ReturnsEveryTargetUnnamed()
        {
            var targets = BufferCapture.ParseTargets(BufferCapture.allTargets);

            Assert.That(Names(targets), Is.EqualTo(UniversalBufferCaptureTargets.all.Select(t => t.name).ToArray()));
            Assert.IsTrue(targets.All(t => !t.named));
        }

        [TestCase("all,depth")]
        [TestCase("depth,all")]
        public void ParseTargets_AllWithNamedTarget_OnlyNamedTargetIsNamed(string buffers)
        {
            var targets = BufferCapture.ParseTargets(buffers);

            Assert.AreEqual(UniversalBufferCaptureTargets.all.Length, targets.Count);
            foreach (var target in targets)
                Assert.AreEqual(target.target.name == "depth", target.named, target.target.name);
        }

        [Test]
        public void ParseTargets_UnknownBuffer_ThrowsAndListsValidBuffers()
        {
            var exception = Assert.Throws<ArgumentException>(() => BufferCapture.ParseTargets("color,nonexistent"));

            StringAssert.Contains("'nonexistent'", exception.Message);
            foreach (var target in UniversalBufferCaptureTargets.all)
                StringAssert.Contains(target.name, exception.Message);
        }
    }

    class BufferCaptureOutputSizeTests
    {
        static Vector2Int GetOutputSize(int cameraWidth, int cameraHeight, bool squareOutput, int width = 0, int height = 0)
        {
            BufferCapture.GetOutputSize(cameraWidth, cameraHeight, squareOutput, width, height, out var w, out var h);
            return new Vector2Int(w, h);
        }

        [Test]
        public void GetOutputSize_NoSize_UsesCameraSize()
        {
            Assert.AreEqual(new Vector2Int(800, 600), GetOutputSize(800, 600, false));
        }

        [Test]
        public void GetOutputSize_ExplicitSize_OverridesCameraSize()
        {
            Assert.AreEqual(new Vector2Int(320, 200), GetOutputSize(800, 600, false, 320, 200));
        }

        [Test]
        public void GetOutputSize_SquareOutput_IgnoresCameraAspect()
        {
            Assert.AreEqual(new Vector2Int(1024, 1024), GetOutputSize(800, 600, true));
            Assert.AreEqual(new Vector2Int(512, 512), GetOutputSize(800, 600, true, 512));
            Assert.AreEqual(new Vector2Int(512, 256), GetOutputSize(800, 600, true, 512, 256));
        }

        [TestCase(0, 0)]
        [TestCase(1, 600)]
        [TestCase(800, 1)]
        public void GetOutputSize_DegenerateCameraSize_UsesFallback(int cameraWidth, int cameraHeight)
        {
            Assert.AreEqual(new Vector2Int(1920, 1080), GetOutputSize(cameraWidth, cameraHeight, false));
        }

        [Test]
        public void GetOutputSize_OversizedOutput_ClampsLongestEdgeAndKeepsAspect()
        {
            Assert.AreEqual(new Vector2Int(8192, 2048), GetOutputSize(16384, 4096, false));
            Assert.AreEqual(new Vector2Int(41, 8192), GetOutputSize(100, 20000, false));
            Assert.AreEqual(new Vector2Int(8192, 8192), GetOutputSize(800, 600, true, 10000, 10000));
        }
    }

    class BufferCaptureObjectIdImageTests
    {
        static readonly Color32 k_Background = new Color32(0, 0, 0, 255);
        static readonly Color32 k_Unresolved = new Color32(255, 255, 255, 255);

        readonly List<UnityEngine.Object> m_Objects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in m_Objects)
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
            }
            m_Objects.Clear();
        }

        GameObject CreateGameObject(string name, Transform parent = null)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(parent, false);
            m_Objects.Add(go);
            return go;
        }

        Material CreateMaterial(string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.IsNotNull(shader, "The URP Unlit shader was not found.");
            var material = new Material(shader) { name = name, hideFlags = HideFlags.HideAndDontSave };
            m_Objects.Add(material);
            return material;
        }

        // Inverse of ObjectIdResult.DecodeIdFromColor
        static Color32 EncodeId(int id) => new Color32((byte)id, (byte)(id >> 8), (byte)(id >> 16), (byte)(id >> 24));

        static Color32[] EncodeIds(params int[] ids) => ids.Select(EncodeId).ToArray();

        static string ToHex(Color32 color) => "#" + ColorUtility.ToHtmlStringRGB(color);

        [Test]
        public void BuildObjectIdImage_ReportsPixelCountsBoundsAndCoverage()
        {
            var a = CreateGameObject("A");
            var b = CreateGameObject("B");
            var idToObject = new UnityEngine.Object[] { null, a, b };

            // 3x2 texture, bottom row first: A A bg / B unresolved A
            var ids = EncodeIds(1, 1, 0, 2, 99, 1);
            var mapping = BufferCapture.BuildObjectIdImage(ids, 3, 2, idToObject, out _);

            Assert.AreEqual(3, mapping.width);
            Assert.AreEqual(2, mapping.height);
            Assert.AreEqual(1, mapping.unresolvedPixelCount);
            Assert.AreEqual(2, mapping.objects.Length);

            var entryA = mapping.objects[0];
            Assert.AreEqual("A", entryA.name);
            Assert.AreEqual(3, entryA.pixelCount);
            Assert.AreEqual(0.5f, entryA.coverage, 1e-6f);
            Assert.AreEqual(0, entryA.bounds.xMin);
            Assert.AreEqual(0, entryA.bounds.yMin);
            Assert.AreEqual(2, entryA.bounds.xMax);
            Assert.AreEqual(1, entryA.bounds.yMax);

            var entryB = mapping.objects[1];
            Assert.AreEqual("B", entryB.name);
            Assert.AreEqual(1, entryB.pixelCount);
            Assert.AreEqual(1f / 6f, entryB.coverage, 1e-6f);
            Assert.AreEqual(0, entryB.bounds.xMin);
            Assert.AreEqual(0, entryB.bounds.yMin);
            Assert.AreEqual(0, entryB.bounds.xMax);
            Assert.AreEqual(0, entryB.bounds.yMax);
        }

        [Test]
        public void BuildObjectIdImage_ColorsPixelsByObject()
        {
            var a = CreateGameObject("A");
            var b = CreateGameObject("B");
            var idToObject = new UnityEngine.Object[] { null, a, b };

            var ids = EncodeIds(1, 1, 0, 2, 99, 1);
            var mapping = BufferCapture.BuildObjectIdImage(ids, 3, 2, idToObject, out var colored);

            Assert.AreEqual(ids.Length, colored.Length);
            Assert.AreEqual(k_Background, colored[2]);
            Assert.AreEqual(k_Unresolved, colored[4]);
            Assert.AreEqual(colored[0], colored[1]);
            Assert.AreEqual(colored[0], colored[5]);
            Assert.AreNotEqual(colored[0], colored[3]);
            Assert.AreEqual(ToHex(colored[0]), mapping.objects[0].color);
            Assert.AreEqual(ToHex(colored[3]), mapping.objects[1].color);
            Assert.AreEqual(ToHex(k_Background), mapping.background);
        }

        [Test]
        public void BuildObjectIdImage_NullMappingEntry_CountsAsUnresolved()
        {
            var a = CreateGameObject("A");
            var idToObject = new UnityEngine.Object[] { null, a, null };

            var mapping = BufferCapture.BuildObjectIdImage(EncodeIds(1, 2), 2, 1, idToObject, out var colored);

            Assert.AreEqual(1, mapping.unresolvedPixelCount);
            Assert.AreEqual(1, mapping.objects.Length);
            Assert.AreEqual(k_Unresolved, colored[1]);
        }

        [Test]
        public void BuildObjectIdImage_HighIdBytes_DecodeToTheRightObject()
        {
            const int k_HighId = 0x010203;
            var a = CreateGameObject("A");
            var idToObject = new UnityEngine.Object[k_HighId + 1];
            idToObject[k_HighId] = a;

            var mapping = BufferCapture.BuildObjectIdImage(EncodeIds(k_HighId), 1, 1, idToObject, out _);

            Assert.AreEqual(0, mapping.unresolvedPixelCount);
            Assert.AreEqual("A", mapping.objects.Single().name);
        }

        [Test]
        public void BuildObjectIdImage_SeveralIdsForOneObject_MergeIntoOneEntry()
        {
            var renderer = CreateGameObject("Multi").AddComponent<MeshRenderer>();
            var idToObject = new UnityEngine.Object[] { null, renderer, renderer };

            var mapping = BufferCapture.BuildObjectIdImage(EncodeIds(1, 2, 2), 3, 1, idToObject, out var colored);

            Assert.AreEqual(3, mapping.objects.Single().pixelCount);
            Assert.AreEqual(colored[0], colored[1]);
            Assert.AreEqual(colored[0], colored[2]);
        }

        [Test]
        public void BuildObjectIdImage_ReportsHierarchyPathAndType()
        {
            var parent = CreateGameObject("Parent");
            var renderer = CreateGameObject("Child", parent.transform).AddComponent<MeshRenderer>();
            var idToObject = new UnityEngine.Object[] { null, renderer };

            var entry = BufferCapture.BuildObjectIdImage(EncodeIds(1), 1, 1, idToObject, out _).objects.Single();

            Assert.AreEqual("Child", entry.name);
            Assert.AreEqual("Parent/Child", entry.path);
            Assert.AreEqual(nameof(MeshRenderer), entry.type);
        }

        [Test]
        public void BuildObjectIdImage_SharedMaterialsAndShaders_AreStoredOnce()
        {
            var shared = CreateMaterial("Shared");
            var other = CreateMaterial("Other");

            var first = CreateGameObject("First").AddComponent<MeshRenderer>();
            first.sharedMaterials = new[] { shared, null };
            var second = CreateGameObject("Second").AddComponent<MeshRenderer>();
            second.sharedMaterials = new[] { shared, other };
            var noRenderer = CreateGameObject("NoRenderer");
            var idToObject = new UnityEngine.Object[] { null, first, second, noRenderer };

            // First gets the most pixels so the entry order is fixed
            var mapping = BufferCapture.BuildObjectIdImage(EncodeIds(1, 1, 1, 2, 2, 3), 6, 1, idToObject, out _);

            Assert.That(mapping.materials.Select(m => m.name), Is.EqualTo(new[] { "Shared", "Other" }));
            Assert.AreEqual(1, mapping.shaders.Length);
            Assert.AreEqual(shared.shader.name, mapping.shaders[0].name);
            Assert.IsTrue(mapping.materials.All(m => m.shader == 0));
            Assert.IsNull(mapping.materials[0].path);

            Assert.That(mapping.objects[0].materials, Is.EqualTo(new[] { 0, -1 }));
            Assert.That(mapping.objects[1].materials, Is.EqualTo(new[] { 0, 1 }));
            Assert.IsEmpty(mapping.objects[2].materials);
        }

        [Test]
        public void BuildObjectIdImage_ObjectsInSeveralScenes_ReferenceSharedSceneEntries()
        {
            // Hidden, unsaved root objects are removed from regular scenes but stay in preview scenes
            var sceneA = EditorSceneManager.NewPreviewScene();
            var sceneB = EditorSceneManager.NewPreviewScene();
            try
            {
                var first = CreateGameObject("First").AddComponent<MeshRenderer>();
                var second = CreateGameObject("Second").AddComponent<MeshRenderer>();
                var third = CreateGameObject("Third").AddComponent<MeshRenderer>();
                SceneManager.MoveGameObjectToScene(first.gameObject, sceneA);
                SceneManager.MoveGameObjectToScene(second.gameObject, sceneA);
                SceneManager.MoveGameObjectToScene(third.gameObject, sceneB);
                var idToObject = new UnityEngine.Object[] { null, first, second, third };

                // First gets the most pixels so the entry order is fixed
                var mapping = BufferCapture.BuildObjectIdImage(EncodeIds(1, 1, 1, 2, 2, 3), 6, 1, idToObject, out _);

                Assert.AreEqual(2, mapping.scenes.Length);
                Assert.That(mapping.objects.Select(o => o.scene), Is.EqualTo(new[] { 0, 0, 1 }));
                Assert.IsNull(mapping.scenes[0].path);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(sceneA);
                EditorSceneManager.ClosePreviewScene(sceneB);
            }
        }

        [Test]
        public void BuildObjectIdImage_ObjectWithoutGameObject_HasNoScene()
        {
            var material = CreateMaterial("NotARenderer");
            var idToObject = new UnityEngine.Object[] { null, material };

            var mapping = BufferCapture.BuildObjectIdImage(EncodeIds(1), 1, 1, idToObject, out _);

            Assert.AreEqual(-1, mapping.objects.Single().scene);
            Assert.IsEmpty(mapping.scenes);
        }

        [Test]
        public void BuildObjectIdImage_ManyObjects_GetDistinctColors()
        {
            const int k_ObjectCount = 300;
            var idToObject = new UnityEngine.Object[k_ObjectCount + 1];
            var ids = new int[k_ObjectCount];
            for (int i = 0; i < k_ObjectCount; ++i)
            {
                idToObject[i + 1] = CreateGameObject("Object" + i);
                ids[i] = i + 1;
            }

            var mapping = BufferCapture.BuildObjectIdImage(EncodeIds(ids), k_ObjectCount, 1, idToObject, out var colored);

            Assert.AreEqual(k_ObjectCount, mapping.objects.Length);
            Assert.AreEqual(k_ObjectCount, colored.Distinct().Count());
            CollectionAssert.DoesNotContain(colored, k_Background);
            CollectionAssert.DoesNotContain(colored, k_Unresolved);
        }
    }
}
