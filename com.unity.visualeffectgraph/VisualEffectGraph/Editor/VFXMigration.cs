using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.VFX;
using UnityEditor.Experimental.VFX;

public class VFXMigration
{
    [MenuItem("VFX Editor/Migrate to .vfx")]
    static void Migrate()
    {
        MigrateFolder("Assets");
        AssetDatabase.Refresh();
    }

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
                var asset = VisualEffectResource.GetResourceAtPath(path);
                if (asset != null)
                {
                    asset.ValidateAsset();
                    try
                    {
                        var graph = asset.GetOrCreateGraph();
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
}
