using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.VFX;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEditor.SceneManagement;

public class VFXMigration
{
    /*
    [MenuItem("VFX Editor/Migrate to .vfx")]
    static void Migrate()
    {
        MigrateFolder("Assets");
        AssetDatabase.Refresh();
    }
    */

    [MenuItem("VFX Editor/Resave All VFX assets")]
    static void Resave()
    {
        ResaveFolder("Assets");

        AssetDatabase.SaveAssets();
    }

    static void MigrateFolder(string dirPath)
    {
        foreach (var path in Directory.GetFiles(dirPath))
        {
            if (Path.GetExtension(path) == ".asset")
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Experimental.VFX.VisualEffectAsset>(path) != null)
                {
                    string pathWithoutExtension = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path);

                    if (!File.Exists(pathWithoutExtension + ".vfx"))
                    {
                        bool success = false;

                        string message = null;
                        for (int i = 0; i < 10 && !success; ++i)
                        {
                            try
                            {
                                File.Move(path, pathWithoutExtension + ".vfx");
                                File.Move(pathWithoutExtension + ".asset.meta", pathWithoutExtension + ".vfx.meta");
                                Debug.Log("renaming " + path + " to " + pathWithoutExtension + ".vfx");
                                success = true;
                            }
                            catch (System.Exception e)
                            {
                                message = e.Message;
                            }
                        }
                        if (!success)
                        {
                            Debug.LogError(" failed renaming " + path + " to " + pathWithoutExtension + ".vfx" + message);
                        }
                    }
                }
            }
        }
        foreach (var path in Directory.GetDirectories(dirPath))
        {
            MigrateFolder(path);
        }
    }

    static void ResaveFolder(string dirPath)
    {
        foreach (var path in Directory.GetFiles(dirPath))
        {
            if (Path.GetExtension(path) == ".vfx")
            {
                VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                if (asset == null)
                {
                    AssetDatabase.ImportAsset(path);
                    asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                }

                var resource = asset.GetResource();
                if (asset != null)
                {
                    resource.ValidateAsset();
                    try
                    {
                        var graph = resource.GetOrCreateGraph();
                        graph.RecompileIfNeeded();
                        EditorUtility.SetDirty(graph);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Couldn't resave vfx" + path + " " + e.Message);
                    }
                }
            }
        }
        foreach (var path in Directory.GetDirectories(dirPath))
        {
            ResaveFolder(path);
        }
    }

    struct ComponentData
    {
        public string assetPath;
        public Dictionary<string, Dictionary<string, object>> values;
    }

    class FileVFXComponents
    {
        public string path;

        public Dictionary<string, ComponentData> componentPaths;
    }


    [MenuItem("VFX Editor/Migrate Components")]
    public static void MigrateComponents()
    {
        List<FileVFXComponents> files = new List<FileVFXComponents>();
        var sceneGuids = AssetDatabase.FindAssets("t:Scene");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); // load a new scene to make sure we don't have multiple scenes loaded

        foreach (var guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            files.Add(FindComponentsInScene());
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); // load a new scene to make sure we don't have multiple scenes loaded

        Resave(); // Convert to the new format with the vfx assets in the library

        foreach (var file in files)
        {
            EditorSceneManager.OpenScene(file.path, OpenSceneMode.Single);
            SetComponentsInScene(file);

            EditorSceneManager.SaveScene(EditorSceneManager.GetSceneByPath(file.path));
        }
    }

    [MenuItem("VFX Editor/Migrate Components in Current Scene")]
    public static void MigrateComponentsCurrentScnene()
    {
        FileVFXComponents components = FindComponentsInScene();


        foreach (var path in components.componentPaths.Values)
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path.assetPath);
            if (asset == null)
            {
                AssetDatabase.ImportAsset(path.assetPath);
                asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path.assetPath);
            }

            var resource = asset.GetResource();
            if (asset != null)
            {
                resource.ValidateAsset();
                try
                {
                    var graph = resource.GetOrCreateGraph();
                    graph.RecompileIfNeeded();
                    EditorUtility.SetDirty(graph);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Couldn't resave vfx" + path.assetPath + " " + e.Message);
                }
            }
        }


        SetComponentsInScene(components);
        EditorSceneManager.SaveScene(EditorSceneManager.GetSceneByPath(components.path));
    }

    static FileVFXComponents FindComponentsInScene()
    {
        var objects = EditorSceneManager.GetActiveScene().GetRootGameObjects();

        FileVFXComponents infos = new FileVFXComponents();
        infos.path = EditorSceneManager.GetActiveScene().path;
        infos.componentPaths = new Dictionary<string, ComponentData>();

        foreach (var obj in objects)
        {
            FindVFXInGameObjectRecurse(infos, obj, "");
        }

        return infos;
    }

    static void FindComponentsInScene(FileVFXComponents infos)
    {
        var objects = EditorSceneManager.GetActiveScene().GetRootGameObjects();

        foreach (var obj in objects)
        {
            FindVFXInGameObjectRecurse(infos, obj, "");
        }
    }

    static void SetComponentsInScene(FileVFXComponents infos)
    {
        var objects = EditorSceneManager.GetActiveScene().GetRootGameObjects();

        foreach (var obj in objects)
        {
            SetVFXInGameObjectRecurse(infos, obj, "");
        }
    }

    static void FindVFXInGameObjectRecurse(FileVFXComponents infos, GameObject go, string path)
    {
        VisualEffect effect = go.GetComponent<VisualEffect>();
        if (effect != null)
        {
            if (!object.ReferenceEquals(effect.visualEffectAsset, null))
            {
                string assetPath = AssetDatabase.GetAssetPath(effect.visualEffectAsset.GetInstanceID());
                string componentPath = path + "/" + effect.name;

                Dictionary<string, Dictionary<string, object>> values = new Dictionary<string, Dictionary<string, object>>();

                SerializedObject obj = new SerializedObject(effect);

                if (assetPath.Contains("Gradient"))
                {
                    Debug.Log("");
                }

                foreach (var setter in m_Setters)
                {
                    string property = "m_PropertySheet." + setter.Key + ".m_Array";

                    SerializedProperty arrayProp = obj.FindProperty(property);
                    if (arrayProp.arraySize > 0)
                    {
                        values[setter.Key] = new Dictionary<string, object>();
                        for (int i = 0; i < arrayProp.arraySize; ++i)
                        {
                            var elementProp = arrayProp.GetArrayElementAtIndex(i);

                            if (elementProp.FindPropertyRelative("m_Overridden").boolValue)
                            {
                                values[setter.Key].Add(elementProp.FindPropertyRelative("m_Name").stringValue, setter.Value.get(elementProp.FindPropertyRelative("m_Value")));
                            }
                        }
                    }
                }

                infos.componentPaths.Add(componentPath, new ComponentData() { assetPath = assetPath, values = values });
            }
        }

        foreach (UnityEngine.Transform child in go.transform)
        {
            FindVFXInGameObjectRecurse(infos, child.gameObject, path + "/" + go.name);
        }
    }

    static void SetVFXInGameObjectRecurse(FileVFXComponents infos, GameObject go, string path)
    {
        VisualEffect effect = go.GetComponent<VisualEffect>();
        if (effect != null)
        {
            string componentPath = path + "/" + effect.name;


            ComponentData componentData;
            if (infos.componentPaths.TryGetValue(componentPath, out componentData))
            {
                //if (effect.visualEffectAsset == null)
                {
                    string assetPath = componentData.assetPath;
                    VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(componentData.assetPath);


                    if (assetPath.Contains("Gradient"))
                    {
                        Debug.Log("");
                    }

                    EditorUtility.SetDirty(effect);
                    effect.visualEffectAsset = asset;
                    effect.SetVisualEffectAssetDirty(true);

                    SerializedObject obj = new SerializedObject(effect);

                    foreach (var value in componentData.values)
                    {
                        string property = "m_PropertySheet." + value.Key + ".m_Array";
                        SerializedProperty arrayProp = obj.FindProperty(property);
                        foreach (var setter in value.Value)
                        {
                            bool found = false;
                            for (int i = 0; i < arrayProp.arraySize; ++i)
                            {
                                var elementProp = arrayProp.GetArrayElementAtIndex(i);

                                if (elementProp.FindPropertyRelative("m_Name").stringValue == setter.Key)
                                {
                                    m_Setters[value.Key].set(elementProp.FindPropertyRelative("m_Value"), setter.Value);
                                    elementProp.FindPropertyRelative("m_Overridden").boolValue = true;
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                Debug.LogWarning("Asset : " + assetPath + " no longer seems to have a parameter " + setter.Key + " of type " + value.Key.Substring(2) + "referenced from "  + componentPath + " in scene" + infos.path);
                            }
                            else
                            {
                                Debug.Log("Asset : " + assetPath + " restored parameter " + setter.Key + " of type " + value.Key.Substring(2) + "referenced from " + componentPath + " in scene" + infos.path);
                            }
                        }
                    }

                    obj.ApplyModifiedProperties();

                    Debug.Log("Restoring component :" + componentPath + "of scene :" + infos.path + " to have asset :" + assetPath);
                }
            }
        }

        foreach (UnityEngine.Transform child in go.transform)
        {
            SetVFXInGameObjectRecurse(infos, child.gameObject, path + "/" + go.name);
        }
    }

    static string GetComponentPath(Component c)
    {
        if (c.transform.parent == null)
            return c.name;

        return GetComponentPath(c.transform.parent) + "/" + c.name;
    }

    struct PropertyInfo
    {
        public System.Action<SerializedProperty, object> set;
        public System.Func<SerializedProperty, object> get;
    }


    static Dictionary<System.Type, string> m_Properties = new Dictionary<System.Type, string>() {
        { typeof(Vector2), "m_Vector2f"},
        { typeof(Vector3), "m_Vector3f"},
        { typeof(Vector4), "m_Vector4f"},
        { typeof(Color), "m_Vector4f"},
        { typeof(AnimationCurve), "m_AnimationCurve"},
        { typeof(Gradient), "m_Gradient"},
        { typeof(Texture2D), "m_NamedObject"},
        { typeof(Texture2DArray), "m_NamedObject"},
        { typeof(Texture3D), "m_NamedObject"},
        { typeof(Cubemap), "m_NamedObject"},
        { typeof(CubemapArray), "m_NamedObject"},
        { typeof(float), "m_Float"},
        { typeof(int), "m_Int"},
        { typeof(uint), "m_Uint"},
        { typeof(bool), "m_Bool"},
        { typeof(Matrix4x4), "m_Matrix4x4f"}
    };


    static Dictionary<string, PropertyInfo> m_Setters = new Dictionary<string, PropertyInfo>()
    {
        {"m_Vector2f", new PropertyInfo() {set = (SerializedProperty p, object o) => p.vector2Value = (Vector2)o, get = (SerializedProperty p) => p.vector2Value} },
        {"m_Vector3f", new PropertyInfo() {set = (SerializedProperty p, object o) => p.vector3Value = (Vector3)o, get = (SerializedProperty p) => p.vector3Value} },
        {"m_Vector4f", new PropertyInfo() {set = (SerializedProperty p, object o) => p.vector4Value = (Vector4)o, get = (SerializedProperty p) => p.vector4Value} },
        {"m_AnimationCurve", new PropertyInfo() {set = (SerializedProperty p, object o) => p.animationCurveValue = (AnimationCurve)o, get = (SerializedProperty p) => p.animationCurveValue} },
        {"m_Gradient", new PropertyInfo() {set = (SerializedProperty p, object o) => p.gradientValue = (Gradient)o, get = (SerializedProperty p) => p.gradientValue} },
        {"m_NamedObject", new PropertyInfo() {set = (SerializedProperty p, object o) => p.objectReferenceValue = (UnityEngine.Object)o, get = (SerializedProperty p) => p.objectReferenceValue} },
        {"m_Float", new PropertyInfo() {set = (SerializedProperty p, object o) => p.floatValue = (float)o, get = (SerializedProperty p) => p.floatValue} },
        {"m_Int", new PropertyInfo() {set = (SerializedProperty p, object o) => p.intValue = (int)o, get = (SerializedProperty p) => p.intValue} },
        {"m_Uint", new PropertyInfo() {set = (SerializedProperty p, object o) => p.longValue = (long)(uint)o, get = (SerializedProperty p) => (uint)p.longValue} },
        {"m_Bool", new PropertyInfo() {set = (SerializedProperty p, object o) => p.boolValue = (bool)o, get = (SerializedProperty p) => p.boolValue} },
    };
}
