using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class PrintAssetGUID
{
    // Add a new menu item that is accessed by right-clicking on an asset in the project view

    [MenuItem("Assets/PrintAssetGUID", priority = 2)]
    private static void CopyImages()
    {
        StringBuilder sb = new StringBuilder(Selection.objects.Length * 20);
        for (int i = 0; i < Selection.objects.Length; i++)
        {
            var path = AssetDatabase.GetAssetPath(Selection.objects[i]);
            var guid = AssetDatabase.AssetPathToGUID(path);
            sb.AppendLine(guid + " : " + path);
        }
        Debug.Log(sb);
    }
}
