using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Test", menuName = "Shader Graph Test Asset")]
public class ShaderGraphTestAsset : ScriptableObject
{
    public List<Material> testMaterial;
    public Mesh customMesh;
    public bool isCameraPerspective;
    public void print()
    {
        Debug.Log("this is a " + testMaterial.GetType());
    }

}
