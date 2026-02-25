#if USING_2DANIMATION
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    [MovedFrom(true, "UnityEngine.Rendering.Universal", "Unity.RenderPipelines.Universal.2D.Runtime", "ShadowShape2DProvider_SpriteSkin")]
    class ShadowCaster2DProvider_SpriteSkin : ShadowCaster2DProvider
    {
        const float k_InitialTrim = 0.05f;

        ShadowShape2D m_PersistantShapeData;
        int           m_LastDeformedVertexHash;

        void TryToSetPersistantShapeData(SpriteSkin spriteSkin, ShadowShape2D persistantShadowShape, bool force)
        {
            if (spriteSkin != null)
                persistantShadowShape.SetShape(spriteSkin.outlineVertices, spriteSkin.outlineIndices, ShadowShape2D.OutlineTopology.Lines);
        }

        void UpdatePersistantShapeData(SpriteRenderer spriteRenderer)
        {
            SpriteSkin spriteSkin;
            spriteRenderer.TryGetComponent<SpriteSkin>(out spriteSkin);
            if (spriteSkin != null)
            {
                TryToSetPersistantShapeData(spriteSkin, m_PersistantShapeData, true);
            }
        }

        //============================================================================================================
        //                                                  Public
        //============================================================================================================
        public override int MenuPriority() { return 10; }  // give higher than default menu priority
        public override bool IsRequiredComponentData(Component sourceComponent) { return sourceComponent is SpriteSkin; }

#if USING_2DANIMATION_13_0_1_OR_ABOVE
        public override void Enabled(Component sourceComponent, ShadowShape2D persistantShadowShape)
        {
            if (sourceComponent.TryGetComponent(out SpriteSkin spriteSkin))
            {
                spriteSkin.RegisterOutlineDependency();
            }
        }

        public override void Disabled(Component sourceComponent, ShadowShape2D persistantShadowShape)
        {
            if (sourceComponent.TryGetComponent(out SpriteSkin spriteSkin))
            {
                spriteSkin.UnregisterOutlineDependency();
            }
        }
#endif
        public override void OnInitialized(Component sourceComponent, ShadowShape2D persistantShadowShape)
        {
            SpriteSkin spriteSkin = (SpriteSkin)sourceComponent;
            SpriteRenderer spriteRenderer;
            spriteSkin.TryGetComponent<SpriteRenderer>(out spriteRenderer);

            float trimEdge = ShadowShapeProvider2DUtility.GetTrimEdgeFromBounds(spriteRenderer.bounds, k_InitialTrim);
            persistantShadowShape.SetDefaultTrim(trimEdge);

            TryToSetPersistantShapeData(spriteSkin, persistantShadowShape, true);
        }

        public override void OnBeforeRender(Component sourceComponent, Bounds worldCullingBounds, ShadowShape2D persistantShadowShape)
        {
            SpriteSkin spriteSkin = (SpriteSkin)sourceComponent;
            if (spriteSkin != null && spriteSkin.vertexDeformationHash != m_LastDeformedVertexHash)
            {
                SpriteRenderer spriteRenderer;
                spriteSkin.TryGetComponent<SpriteRenderer>(out spriteRenderer);
                persistantShadowShape.SetFlip(spriteRenderer.flipX, spriteRenderer.flipY);

                TryToSetPersistantShapeData(spriteSkin, persistantShadowShape, false);
                m_LastDeformedVertexHash = spriteSkin.vertexDeformationHash;
            }
        }
    }
}
#endif
