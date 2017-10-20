using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor.ProjectWindowCallback;
using System.IO;
using System.Text;

namespace UnityEditor.VFX
{
    public class ProjectUtils
    {
        [MenuItem("Assets/Create/VFX Editor/NodeBlock (C#)", priority = 0)]
        public static void MenuCreateVFXBlock()
        {
            var icon = EditorGUIUtility.FindTexture("MonoBehaviour Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateVFXBlockAsset>(), "NewVFXBlock.cs", icon, null);
        }


        internal class DoCreateVFXBlockAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                if (pathName.Contains(@"\Editor\") || pathName.Contains(@"/Editor/"))
                {
                    string templatePath = Application.dataPath + "/VFXEditor/Editor/Utils/Templates/VFXBlock.template";

                    string contents = new StreamReader(File.Open(templatePath, FileMode.Open)).ReadToEnd();
                    contents = contents.Replace("%CLASSNAME%", GetClassName(pathName));
                    File.WriteAllText(pathName, contents);
                    AssetDatabase.ImportAsset(pathName, ImportAssetOptions.Default);
                    AssetDatabase.Refresh();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error","VFX Nodeblocks can only be created inside an Editor subfolder","OK");
                }
            }

            public string GetClassName(string path)
            {
                string str = Path.GetFileNameWithoutExtension(path);
                StringBuilder sb = new StringBuilder();
                foreach (char c in str)
                {
                    if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_')
                    {
                        if (sb.Length == 0 && (c >= '0' && c <= '9')) sb.Append("_");
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }
        }
    }
}


