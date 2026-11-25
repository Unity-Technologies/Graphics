using System.Collections.Generic;
using NUnit.Framework;
using Unity.RenderPipelines.Core.Runtime.Shared;
using UnityEditor;

namespace UnityEngine.Rendering.Tests
{
    class AssetLinkUtilsTests
    {
        const string k_Folder = "Assets/AssetLinkUtilsTests";
        const string k_AssetPath = k_Folder + "/Linked.asset";

        Object m_PreviousSelection;

        [SetUp]
        public void SetUp()
        {
            m_PreviousSelection = Selection.activeObject;
            CoreUtils.EnsureFolderTreeInAssetFilePath(k_AssetPath);
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = m_PreviousSelection;
            AssetDatabase.DeleteAsset(k_Folder);
        }

        [Test]
        [Description("An asset link holds the GUID of the asset, and clicking it selects that asset.")]
        public void AssetLinkSelectsTheLinkedAsset()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, k_AssetPath);
            string guid = AssetDatabase.AssetPathToGUID(k_AssetPath);

            string color = EditorGUIUtility.isProSkin ? "#40a0ff" : "#0000FF";
            Assert.AreEqual($"<link=\"srpasset='{guid}'\"><color={color}><u>Linked</u></color></link>", AssetLinkUtils.AssetLink(profile));

            Selection.activeObject = null;
            AssetLinkUtils.SelectLinkedAsset(new Dictionary<string, string> { { "href", k_AssetPath } });
            Assert.IsNull(Selection.activeObject, "A link of another kind must not select anything");

            AssetLinkUtils.SelectLinkedAsset(new Dictionary<string, string> { { "srpasset", guid } });
            Assert.AreSame(profile, Selection.activeObject, "The link must select the linked asset");
        }
    }
}
