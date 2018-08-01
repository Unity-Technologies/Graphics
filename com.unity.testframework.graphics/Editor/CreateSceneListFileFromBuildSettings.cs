using System.IO;
using System.Linq;
using UnityEngine.TestTools;

namespace UnityEditor.TestTools.Graphics
{
    internal class CreateSceneListFileFromBuildSettings : IPrebuildSetup
    {
        public void Setup()
        {
            File.WriteAllLines("Assets/StreamingAssets/SceneList.txt",
                EditorBuildSettings.scenes.Where(s => s.enabled == true).Select(s => s.path).ToArray());
        }
    }
}