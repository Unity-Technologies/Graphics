using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GraphicTestTools
{
    [MenuItem("Internal/GraphicTest Tools/Make Material Scene Instance")]
    public static void MakeMaterialSceneInstance()
    {
        foreach (Object obj in Selection.objects)
        {
            Renderer rndr = ((GameObject)obj).GetComponent<Renderer>();
            if (rndr != null)
            {
                Material[] mats = rndr.sharedMaterials;

                for (int i = 0; i < mats.Length; ++i)
                {
                    if (mats[i] != null)
                    {
                        //Debug.Log("Instantiate materal " + rndr.sharedMaterials[i].ToString() + " of object " + rndr.gameObject.name);
                        Material mat = Object.Instantiate(rndr.sharedMaterials[i]);
                        mats[i] = mat;
                    }
                }

                rndr.sharedMaterials = mats;
            }
        }
    }

    [MenuItem("Internal/GraphicTest Tools/Update All Material Placers")]
    public static void UpdateAllPlacers()
    {
        MultiMaterialPlacer[] placers = Object.FindObjectsByType<MultiMaterialPlacer>(FindObjectsSortMode.InstanceID);

        for (int i = 0; i < placers.Length; ++i)
        {
            MultiMaterialPlacerEditor.PlaceObjects(placers[i]);
        }
    }

    [MenuItem("Internal/GraphicTest Tools/Auto name Text Objects")]
    public static void AutoNameTextObjects()
    {
        foreach (Object obj in Resources.FindObjectsOfTypeAll(typeof(TextMesh)))
        {
            TextMesh tm = obj as TextMesh;
            string name = tm.text;
            name = name.Replace(System.Environment.NewLine, " ");
            tm.gameObject.name = name;
        }
    }
}
