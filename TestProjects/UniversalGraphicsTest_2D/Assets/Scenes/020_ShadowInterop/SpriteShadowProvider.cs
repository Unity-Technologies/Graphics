using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;


[ExecuteInEditMode]
public class SpriteShadowProvider : MonoBehaviour, IShadowShapes2DProvider
{
    public SpriteRenderer m_SpriteRenderer;

    IShadowShapes2DProvider.ShadowShapes2D m_ShadowShape;


    public void SetShadowShape(IShadowShapes2DProvider.ShadowShapes2D persistantShapeData)
    {
        m_ShadowShape = persistantShapeData;

        Sprite sprite = m_SpriteRenderer?.sprite;
        if (sprite != null)
            m_ShadowShape.SetEdges(sprite.vertices, sprite.triangles, IShadowShapes2DProvider.OutlineTopology.Triangles);
    }

}
