using System.Collections;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VFX;
using UnityEngine.VFX;
using UnityEditor;
using UnityEditor.VFX.UI;
using UnityEditor.ProjectWindowCallback;

using UnityObject = UnityEngine.Object;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEditor
{
    class VFXBuildPreprocessor : IPreprocessBuildWithReport
    {
        int IOrderedCallback.callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            VFXManagerEditor.CheckVFXManager();
        }
    }
    [InitializeOnLoad]
    static class VisualEffectAssetEditorUtility
    {
        private static string m_TemplatePath = null;

        public static string templatePath
        {
            get
            {
                if (m_TemplatePath == null)
                {
                    m_TemplatePath = VisualEffectGraphPackageInfo.assetPackagePath + "/Editor/Templates/";
                }
                return m_TemplatePath;
            }
        }

        static void CheckVFXManagerOnce()
        {
            VFXManagerEditor.CheckVFXManager();
            EditorApplication.update -= CheckVFXManagerOnce;
        }

        static VisualEffectAssetEditorUtility()
        {
            EditorApplication.update += CheckVFXManagerOnce;


            UnityEngine.VFX.VFXManager.activateVFX = true;
        }

        public const string templateAssetName = "SimpleParticleSystem.vfx";
        public const string templateBlockSubgraphAssetName = "DefaultSubgraphBlock.vfxblock";
        public const string templateOperatorSubgraphAssetName = "DefaultSubgraphOperator.vfxoperator";

        public const string editorResourcesFolder = "Editor/UIResources";
        public static string editorResourcesPath => VisualEffectGraphPackageInfo.assetPackagePath + "/" + editorResourcesFolder;

        [MenuItem("GameObject/Visual Effects/Visual Effect", false, 10)]
        public static void CreateVisualEffectGameObject(MenuCommand menuCommand)
        {
            GameObject go = new GameObject(GameObjectUtility.GetUniqueNameForSibling(null, "Visual Effect"));
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            var vfxComp = go.AddComponent<VisualEffect>();

            if (Selection.activeObject != null && Selection.activeObject is VisualEffectAsset)
            {
                vfxComp.visualEffectAsset = Selection.activeObject as VisualEffectAsset;
                vfxComp.startSeed = (uint)Random.Range(int.MinValue, int.MaxValue);
            }

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

            Selection.activeObject = go;
        }

        public static VisualEffectAsset CreateNewAsset(string path)
        {
            return CreateNew<VisualEffectAsset>(path);
        }

        public static T CreateNew<T>(string path) where T : UnityObject
        {
            string emptyAsset =
@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &114350483966674976
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: 7d4c867f6b72b714dbb5fd1780afe208, type: 3}
--- !u!2058629511 &1
VisualEffectResource:
  m_Graph: {fileID: 114350483966674976}
";

            File.WriteAllText(path, emptyAsset);

            AssetDatabase.ImportAsset(path);

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        [MenuItem("Assets/Create/Visual Effects/Visual Effect Graph", false, 306)]
        public static void CreateVisualEffectAsset()
        {
            string templateString = "";
            try
            {
                templateString = System.IO.File.ReadAllText(templatePath + templateAssetName);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Couldn't read template for new vfx asset : " + e.Message);
                return;
            }

            Texture2D texture = EditorGUIUtility.FindTexture(typeof(VisualEffectAsset));
            var action = ScriptableObject.CreateInstance<DoCreateNewVFX>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, "New VFX.vfx", texture, null);
        }

        [MenuItem("Assets/Create/Visual Effects/Visual Effect Defaults", false, 307)]
        public static void CreateVisualEffectDefaults()
        {
            var obj = VFXResources.CreateInstance<VFXResources>();
            obj.SetDefaults();
            AssetDatabase.CreateAsset(obj, "Assets/Visual Effects Defaults.asset");
            Selection.activeObject = obj;
        }

        [MenuItem("Assets/Create/Visual Effects/Visual Effect Defaults", true)]
        public static bool IsCreateVisualEffectDefaultsActive()
        {
            var resources = Resources.FindObjectsOfTypeAll<VFXResources>();
            return resources == null || resources.Length == 0;
        }

        public static void CreateTemplateAsset(string pathName)
        {
            try
            {
                var templateString = System.IO.File.ReadAllText(templatePath + templateAssetName);
                System.IO.File.WriteAllText(pathName, templateString);
            }
            catch (FileNotFoundException)
            {
                CreateNewAsset(pathName);
            }

            AssetDatabase.ImportAsset(pathName);
        }

        internal class DoCreateNewVFX : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                CreateTemplateAsset(pathName);
                var resource = VisualEffectResource.GetResourceAtPath(pathName);
                ProjectWindowUtil.FrameObjectInProjectWindow(resource.asset.GetInstanceID());
            }
        }

        internal class DoCreateNewSubgraphOperator : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var sg = CreateNew<VisualEffectSubgraphOperator>(pathName);
                ProjectWindowUtil.FrameObjectInProjectWindow(sg.GetInstanceID());
            }
        }

        internal class DoCreateNewSubgraphBlock : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var sg = CreateNew<VisualEffectSubgraphBlock>(pathName);
                ProjectWindowUtil.FrameObjectInProjectWindow(sg.GetInstanceID());
            }
        }

        [MenuItem("Assets/Create/Visual Effects/Visual Effect Subgraph Operator", false, 308)]
        public static void CreateVisualEffectSubgraphOperator()
        {
            string fileName = "New VFX Subgraph Operator.vfxoperator";

            CreateVisualEffectSubgraph<VisualEffectSubgraphOperator, DoCreateNewSubgraphOperator>(fileName, templateOperatorSubgraphAssetName);
        }

        [MenuItem("Assets/Create/Visual Effects/Visual Effect Subgraph Block", false, 309)]
        public static void CreateVisualEffectSubgraphBlock()
        {
            string fileName = "New VFX Subgraph Block.vfxblock";

            CreateVisualEffectSubgraph<VisualEffectSubgraphBlock, DoCreateNewSubgraphBlock>(fileName, templateBlockSubgraphAssetName);
        }

        public static void CreateVisualEffectSubgraph<T, U>(string fileName, string templateName) where U : EndNameEditAction
        {
            string templateString = "";

            Texture2D texture = EditorGUIUtility.FindTexture(typeof(T));
            try // try with the template
            {
                templateString = System.IO.File.ReadAllText(templatePath + templateName);

                ProjectWindowUtil.CreateAssetWithContent(fileName, templateString, texture);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Couldn't read template for new visual effect subgraph : " + e.Message);
                var action = ScriptableObject.CreateInstance<U>();

                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, fileName, texture, null);

                return;
            }
        }
    }
}
