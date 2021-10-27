using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalDrawErrorSystem : DecalDrawSystem
    {
        private DecalTechnique m_Technique;

        public DecalDrawErrorSystem(DecalEntityManager entityManager, DecalTechnique technique) : base("DecalDrawErrorSystem.Execute", entityManager)
        {
            m_Technique = technique;
        }

        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk)
        {
            switch (m_Technique)
            {
                case DecalTechnique.DBuffer:
                    return ((decalCachedChunk.passIndexDBuffer == -1) && (decalCachedChunk.passIndexEmissive == -1)) ? 0 : -1;
                case DecalTechnique.ScreenSpace:
                    return decalCachedChunk.passIndexScreenSpace == -1 ? 0 : -1;
                case DecalTechnique.GBuffer:
                    return decalCachedChunk.passIndexGBuffer == -1 ? 0 : -1;
                case DecalTechnique.Invalid:
                    return 0;
                default:
                    return 0;
            }
        }

        protected override Material GetMaterial(DecalEntityChunk decalEntityChunk) => m_EntityManager.errorMaterial;
    }
}
