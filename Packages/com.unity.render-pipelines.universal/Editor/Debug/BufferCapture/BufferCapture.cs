using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.Universal
{
    // Renders a camera offscreen and saves rendering buffers as PNGs, so tools such as AI agents can inspect a frame
    // without changing what the Editor views display.
    static class BufferCapture
    {
        internal const string allTargets = "all";

        const int k_SquareTargetSize = 1024;
        const int k_FallbackWidth = 1920;
        const int k_FallbackHeight = 1080;
        const int k_MaxDimension = 8192;
        const int k_MaxShaderMessages = 3;
        const int k_MaxIdColorRetries = 8;

        internal sealed class Target
        {
            public string name { get; }
            public string description { get; }

            // Full screen debug views fill the output, so atlases get a square output instead of the camera's aspect.
            public bool squareOutput { get; }

            // Data views (depth, normals, smoothness, ...) are stored as raw values, not sRGB-encoded.
            public bool linearOutput { get; }

            // Configures the pipeline for one render request of the camera; the returned scope restores it.
            public Func<Camera, IDisposable> beginCapture { get; }

            public bool isObjectId { get; }

            readonly Func<Camera, string> m_GetUnsupportedReason;

            public Target(string name, string description, bool squareOutput, Func<Camera, IDisposable> beginCapture, bool isObjectId = false, bool linearOutput = false, Func<Camera, string> getUnsupportedReason = null)
            {
                this.name = name;
                this.description = description;
                this.squareOutput = squareOutput;
                this.linearOutput = linearOutput;
                this.beginCapture = beginCapture;
                this.isObjectId = isObjectId;
                m_GetUnsupportedReason = getUnsupportedReason;
            }

            // Why the camera can't produce this target (renderer, shadow settings, ...), or null when it can
            public string GetUnsupportedReason(Camera camera) => m_GetUnsupportedReason?.Invoke(camera);
        }

        internal readonly struct RequestedTarget
        {
            public readonly Target target;

            // False when the target only came from 'all'
            public readonly bool named;

            public RequestedTarget(Target target, bool named)
            {
                this.target = target;
                this.named = named;
            }
        }

        internal sealed class SkippedTarget
        {
            public Target target;
            public string reason;
        }

        [Serializable]
        internal class PixelBounds
        {
            public int xMin;
            public int yMin;
            public int xMax;
            public int yMax;
        }

        [Serializable]
        internal class ObjectIdEntry
        {
            public string color;
            public string name;
            public string path;

            // Index into ObjectIdMapping.scenes, or -1 when the object is not in a scene
            public int scene;
            public string type;
            public string entityId;
            public int pixelCount;
            public float coverage;
            public PixelBounds bounds;

            // Indices into ObjectIdMapping.materials, one per material slot (-1 for an empty slot)
            public int[] materials;
        }

        [Serializable]
        internal class ShaderMessageInfo
        {
            public string severity;
            public string message;
            public string file;
            public int line;
        }

        [Serializable]
        internal class ShaderInfo
        {
            public string name;
            public string path;
            public bool hasError;
            public bool hasWarnings;
            public ShaderMessageInfo[] messages;
        }

        [Serializable]
        internal class MaterialInfo
        {
            public string name;
            public string path;
            public bool hasUnsavedChanges;

            // Index into ObjectIdMapping.shaders, or -1 when the material has no shader
            public int shader;
        }

        [Serializable]
        internal class SceneInfo
        {
            public string name;

            // Null for a scene that has not been saved
            public string path;
        }

        [Serializable]
        internal class ObjectIdMapping
        {
            public string camera;
            public int width;
            public int height;
            public string background;
            public string boundsOrigin;
            public int unresolvedPixelCount;
            public SceneInfo[] scenes;
            public ShaderInfo[] shaders;
            public MaterialInfo[] materials;
            public ObjectIdEntry[] objects;
        }

        internal sealed class CapturedImage
        {
            public Target target;
            public string path;
            public int width;
            public int height;
            public byte[] png;
        }

        internal sealed class CaptureResult
        {
            public readonly List<CapturedImage> images = new List<CapturedImage>();

            // The caller writes the mapping to mappingPath
            public string mappingPath;
            public ObjectIdMapping mapping;
        }

        internal static bool isUniversalRenderPipelineActive
            => GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;

        internal static List<RequestedTarget> ParseTargets(string targetList)
        {
            var available = UniversalBufferCaptureTargets.all;
            var targets = new List<RequestedTarget>();

            var names = (string.IsNullOrWhiteSpace(targetList) ? "color" : targetList)
                .Split(',')
                .Select(n => n.Trim().ToLowerInvariant())
                .Where(n => n.Length > 0);

            foreach (var name in names)
            {
                if (name == allTargets)
                {
                    targets.AddRange(available.Select(t => new RequestedTarget(t, false)));
                    continue;
                }

                var target = Array.Find(available, t => t.name == name);
                if (target == null)
                    throw new ArgumentException($"Unknown buffer '{name}'. Valid buffers: {allTargets}, {string.Join(", ", available.Select(t => t.name))}.");

                targets.Add(new RequestedTarget(target, true));
            }

            return targets
                .GroupBy(t => t.target.name)
                .Select(g => new RequestedTarget(g.First().target, g.Any(t => t.named)))
                .ToList();
        }

        // Targets that only came from 'all' are skipped when unsupported; a named unsupported target fails the request
        internal static List<Target> SelectSupportedTargets(IReadOnlyList<RequestedTarget> requested, Camera camera, List<SkippedTarget> skipped)
        {
            if (!isUniversalRenderPipelineActive)
                throw new InvalidOperationException("The Universal Render Pipeline is not active.");

            var targets = new List<Target>();
            foreach (var request in requested)
            {
                var reason = request.target.GetUnsupportedReason(camera);
                if (reason == null)
                    targets.Add(request.target);
                else if (request.named)
                    throw new InvalidOperationException($"Buffer '{request.target.name}' is not supported for camera '{camera.name}': {reason}");
                else
                    skipped.Add(new SkippedTarget { target = request.target, reason = reason });
            }

            return targets;
        }

        // Captures apply their modes on top of the global Rendering Debugger settings, so modes the user enabled would
        // leak into them, and hiding debug overlays would turn the capture's own modes off
        internal static void ThrowIfRenderingDebuggerActive()
        {
            const string k_Fix = "Disable them in the Rendering Debugger (Window > Analysis > Rendering Debugger), or reset it, and try again.";

            var settings = UniversalRenderPipelineDebugDisplaySettings.Instance;
            if (settings.isHidingDebugOverlays)
                throw new InvalidOperationException($"The Rendering Debugger is hiding debug overlays. {k_Fix}");

            if (!settings.AreAnySettingsActive)
                return;

            var active = new List<string>();
            settings.ForEach(data =>
            {
                if (data.AreAnySettingsActive)
                    active.Add(data.GetType().Name.Replace("DebugDisplaySettings", ""));
            });
            throw new InvalidOperationException($"Rendering Debugger settings are active ({string.Join(", ", active)}). {k_Fix}");
        }

        internal static Camera GetSceneViewCamera()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
                throw new InvalidOperationException("No Scene view is open.");

            // WORKAROUND: the Scene view only applies its pivot, rotation and lens to the camera when it repaints
            // (SceneView.SetupCamera), so the camera is stale after a domain reload or while the Editor is in the
            // background. Force a synchronous repaint first.
            EditorWindowBridge.RepaintImmediately(sceneView);
            return sceneView.camera;
        }

        internal static Camera GetGameViewCamera()
        {
            var camera = Camera.main;
            if (camera == null || IsOverlayCamera(camera))
                camera = Camera.allCameras.FirstOrDefault(c => !IsOverlayCamera(c));
            if (camera == null)
                throw new InvalidOperationException("No enabled Base camera in the open scenes.");
            return camera;
        }

        // URP only renders Overlay cameras as part of a Base camera's stack, so a request for one renders nothing
        internal static void ThrowIfOverlayCamera(Camera camera)
        {
            if (IsOverlayCamera(camera))
                throw new ArgumentException($"'{camera.name}' is an Overlay camera, which only renders as part of a Base camera's stack. Use its Base camera instead.");
        }

        static bool IsOverlayCamera(Camera camera)
        {
            return camera.TryGetComponent<UniversalAdditionalCameraData>(out var data) &&
                data.renderType == CameraRenderType.Overlay;
        }

        internal static CaptureResult Capture(Camera camera, IReadOnlyList<Target> targets, string directory, int width, int height)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("No GPU available (batchmode with -nographics); cannot capture.");

            if (!isUniversalRenderPipelineActive)
                throw new InvalidOperationException("The Universal Render Pipeline is not active.");

            ThrowIfOverlayCamera(camera);

            Directory.CreateDirectory(directory);
            var result = new CaptureResult();

            // Sized from the camera's on-screen viewport, so this must run before the rect is reset below
            var outputSizes = new Vector2Int[targets.Count];
            for (int i = 0; i < targets.Count; ++i)
            {
                GetOutputSize(camera.pixelWidth, camera.pixelHeight, targets[i].squareOutput, width, height, out var w, out var h);
                outputSizes[i] = new Vector2Int(w, h);
            }

            // Debug modes use shader variants that may not be compiled yet; a one-off capture can't wait for async compilation
            bool oldAsyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;

            // Render requests apply the camera's viewport rect within the output texture, so fill the whole output instead
            var prevRect = camera.rect;
            camera.rect = new Rect(0, 0, 1, 1);
            try
            {
                for (int i = 0; i < targets.Count; ++i)
                {
                    var target = targets[i];
                    int w = outputSizes[i].x;
                    int h = outputSizes[i].y;
                    var path = Path.Combine(directory, target.name + ".png");

                    var png = target.isObjectId
                        ? CaptureObjectId(camera, w, h, path, result)
                        : CaptureColor(camera, target, w, h, path);

                    result.images.Add(new CapturedImage { target = target, path = path, width = w, height = h, png = png });
                }
            }
            finally
            {
                camera.rect = prevRect;
                ShaderUtil.allowAsyncCompilation = oldAsyncCompilation;
            }

            return result;
        }

        static byte[] CaptureColor(Camera camera, Target target, int width, int height, string path)
        {
            var readWrite = target.linearOutput ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, readWrite);
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, target.linearOutput);
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = rt };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("The active render pipeline does not support StandardRequest.");

                using (target.beginCapture?.Invoke(camera))
                    RenderPipeline.SubmitRenderRequest(camera, request);

                ReadBack(rt, tex);
                return WritePng(tex, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        static byte[] CaptureObjectId(Camera camera, int width, int height, string path, CaptureResult result)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var idTex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var colorTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var prevTarget = camera.targetTexture;
            try
            {
                var request = new ObjectIdRequest(rt);

                // SupportsRenderRequest creates the pipeline instance, which Camera.SubmitRenderRequest assumes exists
                RenderPipeline.SupportsRenderRequest(camera, request);
                if (RenderPipelineManager.currentPipeline == null)
                    throw new InvalidOperationException("The URP render pipeline instance could not be created.");

                // WORKAROUND: ObjectIdRequest ignores its destination size; the picking fallback (FillPickBuffer) derives
                // the viewport from the camera's current target, so point the camera at the output while it renders.
                camera.targetTexture = rt;
                camera.SubmitRenderRequest(request);
                camera.targetTexture = prevTarget;

                var idToObject = request.result?.idToObjectMapping;
                if (idToObject == null)
                    throw new InvalidOperationException($"The object id render request for camera '{camera.name}' returned no result.");

                ReadBack(rt, idTex);
                var mapping = BuildObjectIdImage(idTex.GetPixels32(), width, height, idToObject, out var colored);
                mapping.camera = camera.name;

                colorTex.SetPixels32(colored);
                colorTex.Apply();
                var png = WritePng(colorTex, path);

                result.mappingPath = Path.ChangeExtension(path, ".json");
                result.mapping = mapping;
                return png;
            }
            finally
            {
                camera.targetTexture = prevTarget;
                UnityEngine.Object.DestroyImmediate(idTex);
                UnityEngine.Object.DestroyImmediate(colorTex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        internal static ObjectIdMapping BuildObjectIdImage(Color32[] ids, int width, int height, UnityEngine.Object[] idToObject, out Color32[] colored)
        {
            // Picking assigns one id per material slot, so several ids can map to the same object
            var entries = new Dictionary<UnityEngine.Object, ObjectIdEntry>();
            var objectColors = new Dictionary<UnityEngine.Object, Color32>();
            var usedColors = new HashSet<int>();
            var materialTable = new MaterialTable();
            var sceneTable = new SceneTable();
            int unresolvedPixelCount = 0;
            colored = new Color32[ids.Length];

            for (int i = 0; i < ids.Length; ++i)
            {
                int id = ObjectIdResult.DecodeIdFromColor(ids[i]);
                if (id == 0)
                {
                    colored[i] = new Color32(0, 0, 0, 255);
                    continue;
                }

                // Indices past the renderer list belong to gizmos or BatchRendererGroup instances
                if (id < 0 || id >= idToObject.Length || idToObject[id] == null)
                {
                    unresolvedPixelCount++;
                    colored[i] = new Color32(255, 255, 255, 255);
                    continue;
                }

                // Texture rows start at the bottom; report bounds with a top-left origin like the PNG
                int x = i % width;
                int y = height - 1 - i / width;
                var obj = idToObject[id];
                if (!entries.TryGetValue(obj, out var entry))
                {
                    var color = GetUniqueIdColor(id, usedColors);
                    objectColors.Add(obj, color);
                    entry = CreateEntry(color, obj, materialTable, sceneTable);
                    entry.bounds = new PixelBounds { xMin = x, yMin = y, xMax = x, yMax = y };
                    entries.Add(obj, entry);
                }

                colored[i] = objectColors[obj];
                entry.pixelCount++;
                entry.bounds.xMin = Math.Min(entry.bounds.xMin, x);
                entry.bounds.yMin = Math.Min(entry.bounds.yMin, y);
                entry.bounds.xMax = Math.Max(entry.bounds.xMax, x);
                entry.bounds.yMax = Math.Max(entry.bounds.yMax, y);
            }

            float pixelTotal = width * height;
            foreach (var entry in entries.Values)
                entry.coverage = entry.pixelCount / pixelTotal;

            var objects = entries.Values.OrderByDescending(e => e.pixelCount).ToArray();
            return new ObjectIdMapping
            {
                width = width,
                height = height,
                background = "#000000",
                boundsOrigin = "top-left",
                unresolvedPixelCount = unresolvedPixelCount,
                scenes = sceneTable.scenes.ToArray(),
                shaders = materialTable.shaders.ToArray(),
                materials = materialTable.materials.ToArray(),
                objects = objects
            };
        }

        static ObjectIdEntry CreateEntry(Color32 color, UnityEngine.Object obj, MaterialTable materialTable, SceneTable sceneTable)
        {
            var go = obj as GameObject ?? (obj as Component)?.gameObject;
            return new ObjectIdEntry
            {
                color = "#" + ColorUtility.ToHtmlStringRGB(color),
                name = go != null ? go.name : obj.name,
                path = go != null ? GetHierarchyPath(go.transform) : null,
                scene = go != null ? sceneTable.GetSceneIndex(go.scene) : -1,
                type = obj.GetType().Name,
                entityId = obj.GetEntityId().ToString(),
                materials = materialTable.GetMaterialIndices(obj),
            };
        }

        sealed class SceneTable
        {
            public readonly List<SceneInfo> scenes = new List<SceneInfo>();
            readonly Dictionary<Scene, int> m_SceneIndices = new Dictionary<Scene, int>();

            public int GetSceneIndex(Scene scene)
            {
                if (!scene.IsValid())
                    return -1;

                if (m_SceneIndices.TryGetValue(scene, out var index))
                    return index;

                index = scenes.Count;
                m_SceneIndices.Add(scene, index);
                scenes.Add(new SceneInfo
                {
                    name = scene.name,
                    path = string.IsNullOrEmpty(scene.path) ? null : scene.path,
                });
                return index;
            }
        }

        // Stores each material and shader once, so the mapping stays small when many objects share them
        sealed class MaterialTable
        {
            public readonly List<ShaderInfo> shaders = new List<ShaderInfo>();
            public readonly List<MaterialInfo> materials = new List<MaterialInfo>();
            readonly Dictionary<Shader, int> m_ShaderIndices = new Dictionary<Shader, int>();
            readonly Dictionary<Material, int> m_MaterialIndices = new Dictionary<Material, int>();

            public int[] GetMaterialIndices(UnityEngine.Object obj)
            {
                if (!(obj is Renderer renderer))
                    return Array.Empty<int>();

                var sharedMaterials = renderer.sharedMaterials;
                var indices = new int[sharedMaterials.Length];
                for (int i = 0; i < sharedMaterials.Length; ++i)
                    indices[i] = sharedMaterials[i] != null ? GetMaterialIndex(sharedMaterials[i]) : -1;
                return indices;
            }

            int GetMaterialIndex(Material material)
            {
                if (m_MaterialIndices.TryGetValue(material, out var index))
                    return index;

                index = materials.Count;
                m_MaterialIndices.Add(material, index);
                materials.Add(new MaterialInfo
                {
                    name = material.name,
                    path = GetAssetPath(material),
                    hasUnsavedChanges = EditorUtility.IsDirty(material),
                    shader = material.shader != null ? GetShaderIndex(material.shader) : -1,
                });
                return index;
            }

            int GetShaderIndex(Shader shader)
            {
                if (m_ShaderIndices.TryGetValue(shader, out var index))
                    return index;

                // ShaderHasError fetches the cached compiler messages that GetShaderMessages returns
                bool hasError = ShaderUtil.ShaderHasError(shader);
                bool hasWarnings = ShaderUtil.ShaderHasWarnings(shader);

                var path = GetAssetPath(shader);
                index = shaders.Count;
                m_ShaderIndices.Add(shader, index);
                shaders.Add(new ShaderInfo
                {
                    name = shader.name,
                    path = path,
                    hasError = hasError,
                    hasWarnings = hasWarnings,
                    messages = hasError || hasWarnings ? GetMessages(shader, path) : Array.Empty<ShaderMessageInfo>(),
                });
                return index;
            }

            // Messages repeat per graphics API; keep the first few distinct ones, errors first.
            // The compiler leaves file empty for errors in the shader asset itself.
            static ShaderMessageInfo[] GetMessages(Shader shader, string shaderPath)
            {
                return ShaderUtil.GetShaderMessages(shader)
                    .OrderBy(m => m.severity == ShaderCompilerMessageSeverity.Error ? 0 : 1)
                    .GroupBy(m => (m.message, m.file, m.line))
                    .Select(g => g.First())
                    .Take(k_MaxShaderMessages)
                    .Select(m => new ShaderMessageInfo
                    {
                        severity = m.severity.ToString(),
                        message = m.message,
                        file = string.IsNullOrEmpty(m.file) ? shaderPath : m.file,
                        line = m.line,
                    })
                    .ToArray();
            }

            static string GetAssetPath(UnityEngine.Object asset)
            {
                var path = AssetDatabase.GetAssetPath(asset);
                return string.IsNullOrEmpty(path) ? null : path;
            }
        }

        // Colors of two ids can still match once the retries run out
        static Color32 GetUniqueIdColor(int id, HashSet<int> usedColors)
        {
            Color32 color = default;
            for (uint attempt = 0; attempt <= k_MaxIdColorRetries; ++attempt)
            {
                color = GetIdColor(id, attempt);
                if (usedColors.Add(color.r | (color.g << 8) | (color.b << 16)))
                    break;
            }

            return color;
        }

        static Color32 GetIdColor(int id, uint attempt)
        {
            const uint k_GoldenRatioHash = 0x9E3779B9u;
            uint hash = ((uint)id + attempt * k_GoldenRatioHash) * k_GoldenRatioHash;
            float hue = (hash >> 8) / (float)(1 << 24);
            float saturation = Mathf.Lerp(0.6f, 0.95f, (hash & 0xF) / 15f);
            float value = Mathf.Lerp(0.7f, 1f, ((hash >> 4) & 0xF) / 15f);
            return Color.HSVToRGB(hue, saturation, value);
        }

        internal static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }

        internal static void GetOutputSize(int cameraWidth, int cameraHeight, bool squareOutput, int width, int height, out int w, out int h)
        {
            if (squareOutput)
            {
                w = width > 0 ? width : k_SquareTargetSize;
                h = height > 0 ? height : w;
            }
            else
            {
                w = width > 0 ? width : cameraWidth;
                h = height > 0 ? height : cameraHeight;
            }

            if (w <= 1 || h <= 1)
            {
                w = k_FallbackWidth;
                h = k_FallbackHeight;
            }

            int longestEdge = Mathf.Max(w, h);
            if (longestEdge > k_MaxDimension)
            {
                float scale = (float)k_MaxDimension / longestEdge;
                w = Mathf.Max(1, Mathf.RoundToInt(w * scale));
                h = Mathf.Max(1, Mathf.RoundToInt(h * scale));
            }
        }

        static void ReadBack(RenderTexture source, Texture2D destination)
        {
            var prevActive = RenderTexture.active;
            RenderTexture.active = source;
            destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            destination.Apply();
            RenderTexture.active = prevActive;
        }

        static byte[] WritePng(Texture2D tex, string path)
        {
            var png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            return png;
        }
    }
}
