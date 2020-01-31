using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class MaterialCreator : MonoBehaviour
{
    [MenuItem("Tools/SG Tests/Materials From Selected Shaders")]
    static void CreateMaterials()
    {
        IEnumerable<Shader> shaders = Selection.objects.Where(s => s is Shader).Cast<Shader>();

        foreach(Shader s in shaders)
        {
            Material m = new Material(s);
            string path = AssetDatabase.GetAssetPath(s);
            path = path.Substring(0, path.LastIndexOf("/"));
            int start = s.name.LastIndexOf("/") + 1;
            path += "/" + s.name.Substring(start, s.name.Length - start) + ".mat";
            AssetDatabase.CreateAsset(m, path);
        }

        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/SG Tests/Get Rid of Underscores")]
    static void KillUnderscores()
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>() ;
        foreach (GameObject go in allObjects)
        {
            go.name = go.name.Replace("_", "");
        }
    }

    [MenuItem("Tools/SG Tests/Add Mats to Scene")]
    static void PlaceMaterialsInScene()
    {
        Transform nodesXform = GameObject.Find("Nodes").transform;

        IEnumerable<Material> matsInScene = nodesXform.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterial);
        
        HashSet<Material> matSet = new HashSet<Material>(matsInScene);

        IEnumerable<Material> matsInSelection = Selection.objects.Where(m => m is Material).Cast<Material>();

        GameObject tempContainer = GameObject.Find("TEMP");

        tempContainer = tempContainer == null ? ObjectFactory.CreateGameObject("TEMP") : tempContainer;
        tempContainer.transform.position = Vector3.zero;
        tempContainer.transform.rotation = Quaternion.identity;
        tempContainer.transform.localScale = Vector3.one;

        int count = matsInSelection.Count();
        float sqrt = Mathf.Sqrt(count);
        int dim = (int)Mathf.Floor(sqrt);
        int x = 0;
        int z = 0;

        foreach(Material m in matsInSelection)
        {
            if(!matSet.Contains(m))
            {
                matSet.Add(m);
                int start = m.shader.name.LastIndexOf("/") + 1;
                string name = m.shader.name.Substring(start, m.shader.name.Length - start);
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = name;
                go.transform.position = new Vector3(x, 0, z);
                go.GetComponent<MeshRenderer>().sharedMaterial = m;
                go.transform.SetParent(tempContainer.transform, true);
                
                ++x;

                if(x > dim)
                {
                    x = 0;
                    ++z;
                }
            }
        }
    }
}
