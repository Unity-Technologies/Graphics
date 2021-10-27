using NUnit.Framework;

namespace UnityEngine.Rendering.Universal.Tests
{
    class DecalTests
    {
        GameObject m_DecalProjector;
        GameObject m_DecalProjector2;
        DecalEntityManager m_EntityManager;
        Shader m_Shader;

        [SetUp]
        public void Setup()
        {
            m_EntityManager = new DecalEntityManager();
            m_DecalProjector = new GameObject("DecalProjector");
            m_DecalProjector2 = new GameObject("DecalProjector");
            m_Shader = Shader.Find("Hidden/InternalErrorShader");
        }

        [TearDown]
        public void Cleanup()
        {
            m_EntityManager.Dispose();
            Object.DestroyImmediate(m_DecalProjector);
            Object.DestroyImmediate(m_DecalProjector2);
        }

        [Test]
        public void DecalDestroyEmptyChunk()
        {
            var decalProjector = m_DecalProjector.AddComponent<DecalProjector>();
            decalProjector.material = new Material(m_Shader);
            var decalProjector2 = m_DecalProjector.AddComponent<DecalProjector>();
            decalProjector2.material = new Material(m_Shader);

            var entity = m_EntityManager.CreateDecalEntity(decalProjector);
            var entity2 = m_EntityManager.CreateDecalEntity(decalProjector2);

            m_EntityManager.DestroyDecalEntity(entity);

            m_EntityManager.Update();

            Assert.AreEqual(1, m_EntityManager.chunkCount);
        }
    }
}
