using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;
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
        }

        static void StandardWarmingLoop(IEnumerable<VFXComponent> vfxComponent)
        {
            foreach (var vfx in vfxComponent)
            {
                var vfxAsset = vfx.vfxAsset;
                if (vfxAsset)
                {
                    vfx.Reinit();
                    vfx.DebugSimulate(10);
                    vfx.pause = true;
                }
            }
        }

        static void WarmingLoopWithAnimator(IEnumerable<VFXComponent> vfxComponent, IEnumerable<Animator> vfxAnimator)
        {
            int loopCount = 10;
            foreach (var vfx in vfxComponent)
            {
                vfx.Reinit();
            }

            AnimationMode.StartAnimationMode();
            for (int loop = 0; loop < loopCount; loop++)
            {
                int sliceCount = 16;
                float simulateTime = 1.0f / (float)sliceCount;
                for (int slice = 0; slice < sliceCount; slice++)
                {
                    float t = (float)slice / (float)sliceCount;
                    foreach (var animator in vfxAnimator)
                    {
                        foreach (var clip in animator.GetCurrentAnimatorClipInfo(0))
                        {
                            AnimationMode.BeginSampling();
                            AnimationMode.SampleAnimationClip(animator.gameObject, clip.clip, t);
                            AnimationMode.EndSampling();
                        }
                    }

                    foreach (var vfx in vfxComponent)
                    {
                        vfx.pause = false;
                        vfx.DebugSimulate(simulateTime);
                        vfx.pause = true;
                    }
                }
            }
            AnimationMode.StopAnimationMode();
        }

        static SceneCaptureInstance InitScene(string scenePath)
        {
            SceneCaptureInstance instance;
            instance.scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            instance.camera = instance.scene.GetRootGameObjects().SelectMany(o => o.GetComponents<Camera>()).First();

            var vfxComponent = instance.scene.GetRootGameObjects().SelectMany(o => o.GetComponents<VFXComponent>());
            var vfxAnimator = instance.scene.GetRootGameObjects().SelectMany(o => o.GetComponents<Animator>());
            var vfxAsset = vfxComponent.Select(o => o.vfxAsset).Where(o => o != null).Distinct();

            foreach (var vfx in vfxAsset)
            {
                var graph = VFXAssetExtensions.GetOrCreateGraph(vfx);
                graph.RecompileIfNeeded();
            }

            if (vfxAnimator.Any())
            {
                WarmingLoopWithAnimator(vfxComponent, vfxAnimator);
            }
            else
            {
                StandardWarmingLoop(vfxComponent);
            }

            instance.camera.cameraType = CameraType.Preview;
            instance.camera.enabled = false;

            instance.camera.renderingPath = RenderingPath.Forward;
            instance.camera.useOcclusionCulling = false;
            instance.camera.scene = instance.scene;

            const int res = 256;
            var renderTexture = new RenderTexture(res, res, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave };
            instance.camera.targetTexture = renderTexture;

            var ambientProbe = RenderSettings.ambientProbe;
            RenderSettings.ambientProbe = ambientProbe;
            RenderTexture.active = renderTexture;
            instance.texture = renderTexture;

            return instance;
        }

        static void CaptureFrameAndClear(SceneCaptureInstance scene, string capturePath)
        {
            RenderTexture.active = scene.texture;
            var captured = new Texture2D(scene.texture.width, scene.texture.height, TextureFormat.ARGB32, false);
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
        }

        static class CollectScene
        {
            public static IEnumerable scenes
            {
                get
                {
                    foreach (var file in Directory.GetFiles("Assets/VFXEditor/Editor/Tests/Scene/", "*.unity"))
                    {
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
            uint waitFrameCount = 16;

            var scenePath = sceneTest.path;
            var treshold = 0.051f;

            var refCapturePath = scenePath.Replace(".unity", ".png");
            var currentCapturePath = scenePath.Replace(".unity", "_fail.png");

            if (!File.Exists(refCapturePath))
            {
                var scene = InitScene(scenePath);
                while (!scene.scene.isLoaded)
                    yield return null;

                for (int i = 0; i < waitFrameCount; ++i)
                {
                    scene.camera.Render();
                    yield return null;
                }
                CaptureFrameAndClear(scene, refCapturePath);
            }

            //Actual capture test
            {
                var scene = InitScene(scenePath);
                while (!scene.scene.isLoaded)
                    yield return null;

                for (int i = 0; i < waitFrameCount; ++i)
                {
                    scene.camera.Render();
                    yield return null;
                }
                CaptureFrameAndClear(scene, currentCapturePath);
            }

            var currentTexture = new Texture2D(2, 2);
            currentTexture.LoadImage(File.ReadAllBytes(currentCapturePath));

            var refTexture = new Texture2D(4, 2);
            refTexture.LoadImage(File.ReadAllBytes(refCapturePath));

            var rmse = CompareTextures(currentTexture, refTexture);
            if (rmse > treshold)
            {
                Assert.Fail(string.Format("Unexpected capture for {0} (treshold : {1}, rmse : {2})", currentCapturePath, treshold, rmse));
            }
            else
            {
                File.Delete(currentCapturePath);
            }
        }
    }
}
