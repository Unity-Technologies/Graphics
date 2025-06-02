using UnityEngine;

public class SetMaterialPropertyBlockOnUpdate : MonoBehaviour
{
    private void Start() {
        m_propertyBlock = new MaterialPropertyBlock();
        m_anim = m_spriteRenderer.gameObject.GetComponent<Animator>();
        if (m_anim)
            m_anim.PlayInFixedTime("Sprite_Fire_Anim", 0, 0.2f);
    }

    private void Update() {

        if (null == m_spriteRenderer)
            return;

        if (m_apply && m_propertyBlock.isEmpty)
        {
            m_propertyBlock.SetColor("_Color", Color.green);
            m_spriteRenderer.SetPropertyBlock(m_propertyBlock);
        }
        else if (!m_apply && !m_propertyBlock.isEmpty)
        {
            m_propertyBlock.Clear();
            m_spriteRenderer.SetPropertyBlock(m_propertyBlock);
        }

        if (m_anim)
            m_anim.PlayInFixedTime("Sprite_Fire_Anim", 0, 0.2f);
    }

//--------------------------------------------------------------------------------------------------------------------------------------------------------------

    MaterialPropertyBlock m_propertyBlock;

//--------------------------------------------------------------------------------------------------------------------------------------------------------------

    Animator m_anim;
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private bool m_apply = false;
}
