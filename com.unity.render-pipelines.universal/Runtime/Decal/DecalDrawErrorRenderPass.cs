using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    public class DecalDrawErrorSystem : DecalDrawSystem
    {
        public DecalDrawErrorSystem(DecalEntityManager entityManager) : base("DecalDrawErrorSystem.Execute", entityManager) {}
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk)
        {
            if (decalCachedChunk.passIndexDBuffer == -1)
                return 0;
            return -1;
        }

        protected override Material GetMaterial(DecalEntityChunk decalEntityChunk) => m_EntityManager.errorMaterial;
    }
}
