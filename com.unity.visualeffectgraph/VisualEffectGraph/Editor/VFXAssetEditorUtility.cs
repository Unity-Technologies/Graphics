using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEditor;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

namespace UnityEditor
{
    [InitializeOnLoad]
    public static class VisualEffectAssetEditorUtility
    {
        private static string m_TemplatePath = null;

        public static string templatePath
        {
            get
            {
                if (m_TemplatePath == null)
                {
                    var guids = AssetDatabase.FindAssets("\"Simple Particle System\" t:VisualEffectAsset");
                    if (guids.Length == 1)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        m_TemplatePath = System.IO.Path.GetDirectoryName(path);
                    }
                    else
                    {
                        string usualPath = "Assets/VFXEditor/Editor/Templates";

                        bool found = false;
                        do
                        {
                            foreach (var guid in guids)
                            {
                                string path = AssetDatabase.GUIDToAssetPath(guid);
                                m_TemplatePath = System.IO.Path.GetDirectoryName(path);
                                if (m_TemplatePath.EndsWith(usualPath))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            int index = m_TemplatePath.IndexOf('/');
                            if (index == -1)
                            {
                                break;
                            }
                            usualPath = usualPath.Substring(index + 1);
                        }
                        while (!found && m_TemplatePath.Length > 0);
                    }
                }
                return m_TemplatePath;
            }
        }


        public const string templateAssetName = "Simple Particle System.vfx";

        [MenuItem("GameObject/Visual Effects/Visual Effect", false, 10)]
        public static void CreateVisualEffectGameObject(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Visual Effect");
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            var vfxComp = go.AddComponent<VisualEffect>();

            if (Selection.activeObject != null && Selection.activeObject is VisualEffectAsset)
            {
                vfxComp.visualEffectAsset = Selection.activeObject as VisualEffectAsset;
                vfxComp.startSeed = (uint)Random.Range(int.MinValue, int.MaxValue);
            }

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        }

        [MenuItem("Assets/Create/Visual Effects/Visual Effect Graph", false, 306)]
        public static void CreateVisualEffectAsset()
        {
            string templateString = "";
            try
            {
                templateString = System.IO.File.ReadAllText(templatePath + "/" + templateAssetName);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Couldn't read template for new vfx asset : " + e.Message);
            }

            ProjectWindowUtil.CreateAssetWithContent("New VFX.vfx", templateString);
        }

        [MenuItem("VFX Editor/Make All Assets Visible")]
        public static void MakeAllVisible()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeInstanceID);

            if (!string.IsNullOrEmpty(path))
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset != null)
                        asset.hideFlags = HideFlags.None;
                }
            }

            AssetDatabase.ImportAsset(path);
        }
    }
}
