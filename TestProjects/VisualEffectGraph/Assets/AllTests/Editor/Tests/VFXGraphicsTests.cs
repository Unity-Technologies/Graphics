using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Collections;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;
using System.IO;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXGraphicsTest
    {
        struct SceneCaptureInstance
        {
            public Camera camera;
            public Scene scene;
            public RenderTexture texture;
            public VisualEffect[] vfxComponents;
            public Animator[] animators;
        }

        static SceneCaptureInstance InitScene(string scenePath)
        {
            SceneCaptureInstance instance;
            instance.scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            instance.camera = instance.scene.GetRootGameObjects().SelectMany(o => o.GetComponents<Camera>()).First();

            var vfxComponent = instance.scene.GetRootGameObjects().SelectMany(o => o.GetComponents<VisualEffect>());
            var animator = instance.scene.GetRootGameObjects().SelectMany(o => o.GetComponents<Animator>());
            var vfxAsset = vfxComponent.Select(o => o.visualEffectAsset).Where(o => o != null).Distinct();

            foreach (var vfx in vfxAsset)
            {
                var graph = vfx.GetResource().GetOrCreateGraph();
                graph.RecompileIfNeeded();
            }

            instance.camera.cameraType = UnityEngine.CameraType.Preview;
            instance.camera.enabled = false;

            instance.camera.renderingPath = RenderingPath.Forward;
            instance.camera.scene = instance.scene;

            const int res = 256;
            var renderTexture = new RenderTexture(res, res, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave };
            renderTexture.name = "VFXGraphicTest";
            instance.camera.targetTexture = renderTexture;

            var ambientProbe = RenderSettings.ambientProbe;
            RenderSettings.ambientProbe = ambientProbe;
            RenderTexture.active = renderTexture;
            instance.texture = renderTexture;

            foreach (var component in vfxComponent)
            {
                component.pause = true;
            }
            instance.vfxComponents = vfxComponent.ToArray();
            instance.animators = animator.ToArray();

            return instance;
        }

        static void StartScene(SceneCaptureInstance instance)
        {
            var vfxComponent = instance.scene.GetRootGameObjects().SelectMany(o => o.GetComponents<VisualEffect>());
            foreach (var component in vfxComponent)
            {
                component.pause = false;
                component.Reinit();
            }
        }

        static void UpdateScene(SceneCaptureInstance instance, float virtualTotalTime)
        {
            if (instance.animators.Length == 0)
                return;

            foreach (var animator in instance.animators)
            {
                foreach (var clip in animator.GetCurrentAnimatorClipInfo(0))
                {
                    AnimationMode.BeginSampling();

                    var range = clip.clip.stopTime - clip.clip.startTime;

                    var normalizedTime = virtualTotalTime / range;
                    var relativeTimeNormalized = normalizedTime - Mathf.Floor(normalizedTime);
                    var relativeTime = clip.clip.startTime + relativeTimeNormalized * range;

                    AnimationMode.SampleAnimationClip(animator.gameObject, clip.clip, relativeTime);
                    AnimationMode.EndSampling();
                }
            }
        }

        static void CaptureFrameAndClear(SceneCaptureInstance scene, string capturePath)
        {
            RenderTexture.active = scene.texture;
            var captured = new Texture2D(scene.texture.width, scene.texture.height, TextureFormat.RGB24, false);
            captured.ReadPixels(new Rect(0, 0, scene.texture.width, scene.texture.height), 0, 0);
            RenderTexture.active = null; //can help avoid errors
            Object.DestroyImmediate(scene.texture, true);
            scene.texture = null;

            var generated = captured.EncodeToPNG();
            if (File.Exists(capturePath))
                File.Delete(capturePath);

            File.WriteAllBytes(capturePath, generated);
        }

        static private float CompareTextures(Texture2D fromDisk, Texture2D captured)
        {
            if (fromDisk == null || captured == null)
                return 1f;

            if (fromDisk.width != captured.width
                || fromDisk.height != captured.height)
                return 1f;

            var pixels1 = fromDisk.GetPixels();
            var pixels2 = captured.GetPixels();
            if (pixels1.Length != pixels2.Length)
                return 1f;

            int numberOfPixels = pixels1.Length;
            float sumOfSquaredColorDistances = 0;
            for (int i = 0; i < numberOfPixels; i++)
            {
                Color p1 = pixels1[i];
                Color p2 = pixels2[i];
                Color diff = p1 - p2;
                diff = diff * diff;
                sumOfSquaredColorDistances += (diff.r + diff.g + diff.b + diff.a) / 4.0f;
            }
            float rmse = Mathf.Sqrt(sumOfSquaredColorDistances / numberOfPixels);
            return rmse;
        }

        public struct SceneTest
        {
            public string path;
            public override string ToString()
            {
                return Path.GetFileName(path);
            }
        }

        static class CollectScene
        {
            public static IEnumerable scenes
            {
                get
                {
                    foreach (var file in Directory.GetFiles("Assets/VFXTests/GraphicsTests/", "*.unity"))
                    {
                        if (file.Contains("MotionVectors")) continue; //disable explicitly instable test
                        yield return new SceneTest
                        {
                            path = file
                        };
                    }
                }
            }
        }

        private static SceneTest[] scenes = CollectScene.scenes.OfType<SceneTest>().ToArray();

        [UnityTest /* TestCaseSource(typeof(CollectScene), "scenes")  <= doesn't work for UnityTest for now */]
        [Timeout(1000 * 10)]
        public IEnumerator RenderSceneAndCompareExpectedCapture([ValueSource("scenes")] SceneTest sceneTest)
        {
            var sceneView = EditorWindow.GetWindow(typeof(SceneView));
            if (sceneView != null)
                sceneView.Close();
            EditorApplication.ExecuteMenuItem("Window/General/Game");

            float simulateTime = 6.0f;
            float frequency = 1.0f / 20.0f;
            uint waitFrameCount = (uint)(simulateTime / frequency);

            var scenePath = sceneTest.path;
            var threshold = 0.01f;

            var refCapturePath = scenePath.Replace(".unity", ".png");
            var currentCapturePath = scenePath.Replace(".unity", "_fail.png");

            VFXManager.updateMode = VFXManagerUpdateMode.Force20Hz;

            var passes = new List<string>();
            if (!File.Exists(refCapturePath))
            {
                passes.Add(refCapturePath);
            }
            passes.Add(currentCapturePath);

            foreach (var pass in passes)
            {
                var scene = InitScene(scenePath);
                while (!scene.scene.isLoaded)
                    yield return null;

                StartScene(scene);

                AnimationMode.StartAnimationMode();

                uint startFrameIndex = VFXManager.frameIndex;
                uint expectedFrameIndex = startFrameIndex + waitFrameCount;
                while (VFXManager.frameIndex != expectedFrameIndex)
                {
                    EditorWindow.GetWindow(typeof(GameView)).Focus();
                    UpdateScene(scene, (VFXManager.frameIndex - startFrameIndex) * frequency);
                    scene.camera.Render();
                    yield return null;
                }

                AnimationMode.StopAnimationMode();
                CaptureFrameAndClear(scene, pass);
            }

            var currentTexture = new Texture2D(2, 2);
            currentTexture.LoadImage(File.ReadAllBytes(currentCapturePath));

            var refTexture = new Texture2D(4, 2);
            refTexture.LoadImage(File.ReadAllBytes(refCapturePath));

            var rmse = CompareTextures(currentTexture, refTexture);
            if (rmse > threshold)
            {
                Assert.Fail(string.Format("Unexpected capture for {0} (threshold : {1}, rmse : {2})", currentCapturePath, threshold, rmse));
            }
            else
            {
                File.Delete(currentCapturePath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            VFXManager.updateMode = VFXManagerUpdateMode.Default;
        }
    }
}
