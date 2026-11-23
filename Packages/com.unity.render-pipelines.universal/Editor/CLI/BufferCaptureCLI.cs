using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.CLI
{
    /// <summary>
    /// CLI adapter for BufferCapture: renders a camera offscreen and saves rendering buffers as PNGs.
    /// </summary>
    static class BufferCaptureCLI
    {
        internal enum View
        {
            Game,
            Scene,
        }

        [Serializable]
        internal class TargetDescription
        {
            public string name;

            // Null when no camera could be resolved, so support wasn't checked
            public bool? supported;
            public string unsupportedReason;
            public string description;
        }

        [Serializable]
        internal class SkippedTarget
        {
            public string buffer;
            public string reason;
        }

        [Serializable]
        internal class CapturedImage
        {
            public string buffer;
            public string savedPath;
            public int width;
            public int height;
            public string base64;
        }

        [Serializable]
        internal class ObjectIdOutput
        {
            public string mappingPath;
            public int objectCount;
        }

        [Serializable]
        internal class CaptureResult
        {
            public string view;
            public string camera;
            public string cameraPath;
            public string outputDir;
            public CapturedImage[] images;

            // Buffers from 'all' that the camera can't produce
            public SkippedTarget[] skipped;

            // Null when object_id was not captured
            public ObjectIdOutput objectId;
        }

        [CliCommand("urp", "list_buffers", "List the buffers 'urp capture_buffers' can produce, and whether the camera supports each one. unsupportedReason says why not (renderer type, shadow settings, ...).", MainThreadRequired = true, Tags = new[] { "capture" }, Effect = CommandEffect.ReadOnly)]
        public static ExitCode ListBuffers(CommandContext cmd,
            [CliArg("view", "Camera to check support for when 'camera' is not set: 'scene' (last active Scene view) or 'game' (Camera.main).")] View view = View.Game,
            [CliArg("camera", "Handle of a Camera or its GameObject (hierarchy path, globalId or instanceId). Overrides 'view'.")] ObjectRef camera = null)
        {
            if (!BufferCapture.isUniversalRenderPipelineActive)
                throw new InvalidOperationException("The Universal Render Pipeline is not active.");

            BufferCapture.ThrowIfRenderingDebuggerActive();

            Camera cam = null;
            try
            {
                cam = ResolveCamera(view, camera, out _);
            }
            catch (InvalidOperationException e) when (camera == null || camera.IsEmpty)
            {
                // Still list the buffers when the view has no camera
                cmd.Std.Error.WriteLine($"{e.Message} Buffer support was not checked.");
            }

            foreach (var t in UniversalBufferCaptureTargets.all)
            {
                var reason = cam != null ? t.GetUnsupportedReason(cam) : null;
                cmd.Result.WriteRecord(new TargetDescription
                {
                    name = t.name,
                    supported = cam != null ? reason == null : (bool?)null,
                    unsupportedReason = reason,
                    description = t.description,
                });
            }
            return ExitCode.Success;
        }

        [CliCommand("urp", "capture_buffers", "Render a camera offscreen and save rendering buffers (color, depth, albedo, normal, object_id, ...) as PNGs. Does not change what the Editor views display. Example: urp capture_buffers --buffers color,object_id --view scene. --buffers takes a comma-separated list or 'all' (see 'urp list_buffers'). object_id also writes a JSON file mapping colors to GameObjects, with each object's materials and shaders and their status (unsaved changes, shader compile errors). Files go to Temp/CaptureBuffer/<timestamp> unless --output-dir is set; files already there are overwritten. Fails while any Rendering Debugger setting is active.", MainThreadRequired = true, Tags = new[] { "capture" }, Effect = CommandEffect.Destructive)]
        public static CaptureResult CaptureBuffers(
            [CliArg("buffers", "Comma-separated buffers to capture, or 'all' for every buffer. Run 'urp list_buffers' for the list, e.g. color, object_id, depth, albedo, normal.")] string buffers = "color",
            [CliArg("view", "Camera to use when 'camera' is not set: 'scene' (last active Scene view) or 'game' (Camera.main).")] View view = View.Game,
            [CliArg("camera", "Handle of a Camera or its GameObject (hierarchy path, globalId or instanceId, e.g. from 'gameobject find --type Camera'). Overrides 'view'.")] ObjectRef camera = null,
            [CliArg("width", "Output width in pixels. 0 uses the camera's pixel width.")] int width = 0,
            [CliArg("height", "Output height in pixels. 0 uses the camera's pixel height.")] int height = 0,
            [CliArg("output-dir", "Folder to write <buffer>.png files to: absolute, or relative to the project root. Defaults to Temp/CaptureBuffer/<timestamp>. Existing files are overwritten. Only files under Assets/ are imported as assets.")] string outputDir = null,
            [CliArg("include-inline-image", "Also return each image inline as base64.")] bool includeInlineImage = false)
        {
            try
            {
                if (!BufferCapture.isUniversalRenderPipelineActive)
                    throw new InvalidOperationException("The Universal Render Pipeline is not active.");
                BufferCapture.ThrowIfRenderingDebuggerActive();

                var requested = BufferCapture.ParseTargets(buffers);
                var cam = ResolveCamera(view, camera, out var resolvedView);
                var skipped = new List<BufferCapture.SkippedTarget>();
                var targets = BufferCapture.SelectSupportedTargets(requested, cam, skipped);
                var directory = ResolveOutputDirectory(outputDir);
                var capture = BufferCapture.Capture(cam, targets, directory, width, height);
                if (capture.mapping != null)
                    File.WriteAllText(capture.mappingPath, JsonConvert.SerializeObject(capture.mapping, Formatting.Indented));

                var written = capture.images.Select(i => i.path).ToList();
                if (capture.mapping != null)
                    written.Add(capture.mappingPath);
                ImportIfUnderAssets(written);

                return new CaptureResult
                {
                    view = resolvedView,
                    camera = cam.name,
                    cameraPath = cam.gameObject.scene.IsValid() ? "/" + BufferCapture.GetHierarchyPath(cam.transform) : null,
                    outputDir = directory,
                    images = capture.images.Select(i => new CapturedImage
                    {
                        buffer = i.target.name,
                        savedPath = i.path,
                        width = i.width,
                        height = i.height,
                        base64 = includeInlineImage ? Convert.ToBase64String(i.png) : null,
                    }).ToArray(),
                    skipped = skipped.Select(s => new SkippedTarget { buffer = s.target.name, reason = s.reason }).ToArray(),
                    objectId =capture.mapping == null ? null : new ObjectIdOutput
                    {
                        mappingPath = capture.mappingPath,
                        objectCount = capture.mapping.objects.Length,
                    },
                };
            }
            catch (Exception e) when (!(e is ArgumentException || e is InvalidOperationException))
            {
                Debug.LogException(e);
                throw;
            }
        }

        static Camera ResolveCamera(View view, ObjectRef cameraHandle, out string resolvedView)
        {
            if (cameraHandle != null && !cameraHandle.IsEmpty)
            {
                resolvedView = "camera";
                if (!ObjectResolver.TryResolve(cameraHandle, out var obj, out var error))
                    throw new ArgumentException(error);

                var cam = obj as Camera ?? (obj as GameObject)?.GetComponent<Camera>();
                if (cam == null)
                    throw new ArgumentException($"'{obj.name}' is not a Camera or a GameObject with a Camera.");
                BufferCapture.ThrowIfOverlayCamera(cam);
                return cam;
            }

            resolvedView = view.ToString().ToLowerInvariant();
            switch (view)
            {
                case View.Scene:
                    return BufferCapture.GetSceneViewCamera();
                case View.Game:
                    return BufferCapture.GetGameViewCamera();
                default:
                    throw new ArgumentException($"Invalid view '{view}'. Expected 'scene' or 'game'.");
            }
        }

        static string projectRoot => Path.GetFullPath(Path.GetDirectoryName(Application.dataPath));

        static string ResolveOutputDirectory(string outputDir)
        {
            if (string.IsNullOrWhiteSpace(outputDir))
            {
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
                return Path.Combine(projectRoot, "Temp", "CaptureBuffer", stamp);
            }

            return Path.GetFullPath(Path.IsPathRooted(outputDir) ? outputDir : Path.Combine(projectRoot, outputDir));
        }

        // Only files under Assets are assets; anywhere else they stay plain files without a .meta
        static void ImportIfUnderAssets(IEnumerable<string> paths)
        {
            var assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            var assetPaths = paths
                .Where(p => p.StartsWith(assets, StringComparison.OrdinalIgnoreCase))
                .Select(p => "Assets/" + p.Substring(assets.Length).Replace(Path.DirectorySeparatorChar, '/'))
                .ToList();
            if (assetPaths.Count == 0)
                return;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var assetPath in assetPaths)
                {
                    AssetDatabase.ImportAsset(assetPath);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}
