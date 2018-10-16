/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Experimental.Rendering.LightweightRP;


public class LWSceneConverter : MonoBehaviour 
{
    [SerializeField]
    static List<Material> _materials;

	//[MenuItem("RenderPipeline/Lightweight Pipeline/Material Upgraders/Convert Legacy Scene Materials", false, 3)]
    static void ConvertToLWPipe()
    {
        _materials = new List<Material>();
        CollectMaterialsInScene();
        BackUpMaterials();
        
        Selection.objects = _materials.ToArray();
        //StandardToLightweightMaterialUpgrader.UpgradeMaterialsToLDSelection();
        GraphicsSettings.renderPipelineAsset = (RenderPipelineAsset)AssetDatabase.LoadMainAssetAtPath("Assets/ScriptableRenderPipeline/LightweightPipline/LightweightPipelineAsset.asset");
    }
	static void CollectMaterialsInScene ()
    {
        _materials.Add(RenderSettings.skybox);

        Scene _scene = SceneManager.GetActiveScene();
        GameObject[] rootItems = _scene.GetRootGameObjects();

        List<Renderer> renderers = new List<Renderer>();
		foreach(GameObject go in rootItems)
		{
            renderers.AddRange(go.GetComponentsInChildren<Renderer>());
        }

		foreach(Renderer rend in renderers)
		{
            if (rend != null)
            {
                Material[] mat = rend.sharedMaterials;
                for (int i = 0; i < mat.Length; i++)
                {
                    if (!_materials.Contains(mat[i]))
                    {
                        if (mat[i] != null)
                        {
                            _materials.Add(mat[i]);
                        }
                    }
                }
            }
        }
    }

    static void BackUpMaterials()
    {
        foreach (Material mat in _materials)
        {
            string path = AssetDatabase.GetAssetPath(mat);
            string newPath = GetBackupPath(path);

            AssetDatabase.CopyAsset(path, newPath);
        }
    }

    //[MenuItem("RenderPipeline/Lightweight Pipeline/Material Upgraders/Revert Legacy Scene Materials", false, 4)]
    static void RevertMaterials(){
        CollectMaterialsInScene();

        foreach (Material mat in _materials)
        {
            string path = AssetDatabase.GetAssetPath(mat);
            string backupPath = GetBackupPath(path);

            Material lwMat = (Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material));
            Material backupMat = (Material)AssetDatabase.LoadAssetAtPath(backupPath, typeof(Material));

            lwMat.CopyPropertiesFromMaterial(backupMat);
        }
        GraphicsSettings.renderPipelineAsset = null;
    }

    static string GetBackupPath(string path){
        string[] splitPath = path.Split(new char[] { '/' });
        splitPath[splitPath.Length - 2] = "Backup";
        string newPath = splitPath[0];
        for (int i = 1; i < splitPath.Length; i++)
        {
            newPath += "/" + splitPath[i];
        }
        return newPath;
    }

}
*/
