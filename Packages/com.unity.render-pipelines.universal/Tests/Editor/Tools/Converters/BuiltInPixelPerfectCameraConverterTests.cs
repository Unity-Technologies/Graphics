using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Tools
{
    [Category("Graphics Tools")]
    class BuiltInPixelPerfectCameraConverterTests
    {
        const string k_PrefabPath = "Assets/BuiltInPixelPerfectCameraConverterTests.prefab";

        GameObject m_Prefab;

        [SetUp]
        public void SetUp()
        {
            var root = new GameObject("Root");
            for (int i = 0; i < 2; ++i)
            {
                var child = new GameObject($"Camera{i.ToString()}", typeof(Camera));
                child.transform.SetParent(root.transform);
            }

            PrefabUtility.SaveAsPrefabAsset(root, k_PrefabPath);
            Object.DestroyImmediate(root);

            m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabPath);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(k_PrefabPath);
        }

        // A scan item stands for one camera, but the upgrade runs over the whole prefab or scene it lives in.
        [Test]
        public void FindComponent_WithSeveralCandidates_ReturnsTheOneWithTheGivenGlobalObjectId()
        {
            var expected = m_Prefab.transform.Find("Camera1").GetComponent<Camera>();
            var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(expected.gameObject).ToString();

            var found = BuiltInPixelPerfectCameraConverter.FindComponent<Camera>(m_Prefab, globalObjectId);

            Assert.AreEqual(expected, found);
        }

        [Test]
        public void FindComponent_WithNoCandidateMatching_ReturnsNull()
        {
            var unrelated = new GameObject("Unrelated", typeof(Camera));
            try
            {
                var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(unrelated).ToString();

                Assert.IsNull(BuiltInPixelPerfectCameraConverter.FindComponent<Camera>(m_Prefab, globalObjectId));
            }
            finally
            {
                Object.DestroyImmediate(unrelated);
            }
        }
    }
}
