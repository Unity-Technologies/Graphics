using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

public class SetUserShaderValueForceDynamicBatching : MonoBehaviour
{
    public SpriteShapeRenderer m_SpriteShapeRenderer;
    public TilemapRenderer m_TilemapRenderer;
    public SpriteRenderer m_SpriteRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_SpriteShapeRenderer.SetShaderUserValue(1045220557);   // ~0.2
        m_TilemapRenderer.SetShaderUserValue(1053609165);       // ~0.4
        m_SpriteRenderer.SetShaderUserValue(1053609165);        // ~0.8

        var mbp = new MaterialPropertyBlock();
        m_SpriteShapeRenderer.GetPropertyBlock(mbp);
        mbp.SetColor("ForceDynamicBatching", Color.red);
        m_SpriteShapeRenderer.SetPropertyBlock(mbp);

        mbp = new MaterialPropertyBlock();
        m_SpriteRenderer.GetPropertyBlock(mbp);
        mbp.SetColor("ForceDynamicBatching", Color.red);
        m_SpriteRenderer.SetPropertyBlock(mbp);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
