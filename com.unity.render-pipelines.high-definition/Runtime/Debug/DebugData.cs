using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;

#endif

namespace UnityEngine.Rendering.HighDefinition
{
    static partial class DebugData
    {
        [Conditional("UNITY_EDITOR")]
        public static void StartRenderRequest(HDCamera camera, CameraSettings renderRequestCameraSettings)
        {
#if UNITY_EDITOR
            InternalStartRenderRequest(camera, renderRequestCameraSettings);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void AddDirectionalLight(Light lightComponent, DirectionalLightData lightData)
        {
#if UNITY_EDITOR
            InternalAddDirectionalLight(lightComponent, lightData);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void AddLight(Light lightComponent, LightData lightData)
        {
#if UNITY_EDITOR
            InternalAddLight(lightComponent, lightData);
#endif
        }
    }
}

#if UNITY_EDITOR
namespace UnityEngine.Rendering.HighDefinition
{
    static partial class DebugData
    {
        const string k_Path = "Captures/lightinfo.txt";

        static List<CameraRenderData> s_Datas = new();

        static readonly string[] k_Header = { Header.Frame, Header.RenderPath, Header.LightPath, Header.LightType, Header.ColorR, Header.ColorG, Header.ColorB };

        static DebugData()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
        }

        static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingEditMode)
            {
                if (File.Exists(k_Path))
                {
                    var parent = Directory.GetParent(k_Path);
                    var files = Directory.GetFiles(parent.FullName);
                    var root = Directory.GetParent(Application.dataPath).FullName + "\\";
                    for (var i = 0; i < files.Length; i++)
                    {
                        files[i] = files[i].Replace(root, "").Replace("\\", "/");
                    }
                    var path = ObjectNames.GetUniqueName(files, k_Path);
                    File.Move(k_Path, path);
                }
            }
        }

        static void Update()
        {
            if (!EditorApplication.isPlaying)
                return;

            var writeHeader = !File.Exists(k_Path);
            var writer = File.AppendText(k_Path);

            if (writeHeader) writer.WriteLine(string.Join(";", k_Header));

            foreach (var cameraRenderData in s_Datas) cameraRenderData.Write(writer);

            writer.Dispose();
        }

        static void InternalStartRenderRequest(HDCamera camera, CameraSettings renderRequestCameraSettings)
        {
            if (!EditorApplication.isPlaying)
                return;

            s_Datas.Add(new CameraRenderData
            {
                Frame = Time.frameCount,
                Path = camera.camera.GetPathCached(),
                InstanceID = camera.camera.GetInstanceID()
            });
        }

        static void InternalAddDirectionalLight(Light lightComponent, DirectionalLightData lightData)
        {
            if (!EditorApplication.isPlaying)
                return;

            var data = s_Datas[s_Datas.Count - 1];
            data.Lights.Add(new LightRenderData
            {
                Color = lightData.color,
                InstanceID = lightComponent.GetInstanceID(),
                LightType = LightType.Directional,
                Path = lightComponent.GetPathCached()
            });
        }

        static void InternalAddLight(Light lightComponent, LightData lightData)
        {
            if (!EditorApplication.isPlaying)
                return;

            var data = s_Datas[s_Datas.Count - 1];
            data.Lights.Add(new LightRenderData
            {
                Color = lightData.color,
                InstanceID = lightComponent.GetInstanceID(),
                LightType = lightComponent.type,
                Path = lightComponent.GetPathCached()
            });
        }

        static void WriteLine(StreamWriter writer, Dictionary<string, string> line)
        {
            var first = true;
            foreach (var key in k_Header)
            {
                if (!first)
                    writer.Write(";");
                first = false;

                if (line.TryGetValue(key, out var token)) writer.Write(token);
            }

            writer.WriteLine();
        }

        static class Header
        {
            public const string Frame = "Frame";
            public const string Render = "Render";
            public const string RenderPath = "RenderPath";
            public const string LightPath = "LightPath";
            public const string PositionX = "X";
            public const string PositionY = "Y";
            public const string PositionZ = "Z";
            public const string ColorR = "R";
            public const string ColorG = "G";
            public const string ColorB = "B";
            public const string LightType = "Type";
        }

        class CameraRenderData
        {
            public int InstanceID;
            public List<LightRenderData> Lights = new();
            public string Path;
            public int Frame;

            public void Write(StreamWriter writer)
            {
                using (DictionaryPool<string, string>.Get(out var line))
                {
                    foreach (var light in Lights)
                    {
                        line.Clear();
                        light.Write(line);
                        line[Header.RenderPath] = Path;
                        line[Header.Frame] = Frame.ToString();

                        WriteLine(writer, line);
                    }
                }
            }
        }

        class LightRenderData
        {
            public Vector3 Color;
            public int InstanceID;
            public LightType LightType;
            public string Path;

            public void Write(Dictionary<string, string> line)
            {
                line[Header.ColorR] = Color.x.ToString();
                line[Header.ColorG] = Color.y.ToString();
                line[Header.ColorB] = Color.z.ToString();
                line[Header.LightType] = LightType.ToString();
                line[Header.LightPath] = Path;
            }
        }
    }
}

static class GameObjectUtilities
{
    static Dictionary<UnityEngine.Object, string> s_PathCache = new();
    static Dictionary<UnityEngine.Object, string> s_NameCache = new();

    public static string GetNameCached(this Component component)
    {
        if (!s_PathCache.TryGetValue(component, out var name))
        {
            name = component.name;
            s_NameCache[component] = name;
        }

        return name;
    }

    public static string GetPathCached(this Component component)
    {
        if (!s_PathCache.TryGetValue(component, out var path))
        {
            using (ListPool<string>.Get(out var tokens))
            {
                var tokenTotalLength = 0;
                var tr = component.transform;
                while (true)
                {
                    var name = tr.GetNameCached();
                    tokens.Add(name);
                    tokenTotalLength += name.Length;

                    if (tr.parent == null)
                        break;
                    tr = tr.parent;
                }
                tokens.Add(tr.gameObject.scene.name);
                tokens.Reverse();

                var sb = new StringBuilder(tokenTotalLength + tokens.Count - 1);
                var first = true;
                foreach (var token in tokens)
                {
                    if (!first)
                        sb.Append("/");
                    first = false;

                    sb.Append(token);
                }
                path = sb.ToString();

                s_PathCache[component] = path;
            }
        }

        return path;
    }
}
#endif
