using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GraphicTestTools
{
    [MenuItem("GraphicTest Tools/Make Material Scene Instance")]
    public static void MakeMaterialSceneInstance()
    {
        foreach(Object obj in Selection.objects)
        {
            Renderer rndr = ((GameObject)obj).GetComponent<Renderer>();
            if(rndr!=null)
            {
                Material[] mats = rndr.sharedMaterials;

                for (int i=0; i< mats.Length; ++i)
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
}
