using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class CompressBC6HAndDisplay : MonoBehaviour
{
    [SerializeField]
    ComputeShader m_BC6HShader;
    [SerializeField]
    Texture m_SourceTexture;
    [SerializeField]
    Material m_SourceMaterial;
    [SerializeField]
    string m_TextureName = "_ColorMap";

    int m_Hash = 0;
    RenderTargetIdentifier m_SourceId;
    Texture m_Target;
    RenderTargetIdentifier m_TargetId;

    int m_MipCount = 0;
    Renderer m_Renderer;
    Material m_Material;
    EncodeBC6H m_EncodeBC6H;

    void OnEnable()
    {
        if (m_Material != null)
            DestroyImmediate(m_Material);

        m_Material = Instantiate(m_SourceMaterial);
        var renderer = m_Renderer ?? (m_Renderer = GetComponent<Renderer>());
        renderer.material = m_Material;

        m_EncodeBC6H = new EncodeBC6H(m_BC6HShader);
    }

    void EncodeBC6HTest(CommandBuffer cmb)
    {
        if (m_SourceTexture == null
            || m_SourceMaterial == null
            || m_BC6HShader == null)
        {
            enabled = false;
            return;
        }

        using (new ProfilingSample(cmb, "EncodeBC6H Test"))
        {
            var hash = CalculateHash(m_SourceTexture);
            if (m_Hash != hash)
            {
                m_Hash = hash;
                m_SourceId = new RenderTargetIdentifier(m_SourceTexture);
                CreateTargetInstance();
            }

            m_EncodeBC6H.EncodeFastCubemap(cmb, m_SourceId, m_SourceTexture.width, m_TargetId, 0, m_MipCount - 1);
        }

        m_Material.SetTexture(m_TextureName, m_Target);
    }

    void Update()
    {
        var cmd = new CommandBuffer { name = "EncodeBC6H Compress" };
        EncodeBC6HTest(cmd);
        Graphics.ExecuteCommandBuffer(cmd);
    }

    [ContextMenu("Create Target")]
    void CreateTargetInstance()
    {
        if (m_Target is RenderTexture)
            ((RenderTexture)m_Target).Release();

        var t = new Cubemap(m_SourceTexture.width, TextureFormat.BC6H, true);
        m_Target = t;

        m_Material.SetTexture(m_TextureName, m_Target);
        m_SourceId = new RenderTargetIdentifier(m_SourceTexture);
        m_TargetId = new RenderTargetIdentifier(m_Target);
        m_MipCount = ((Cubemap)m_SourceTexture).mipmapCount;
    }

    static int CalculateHash(Texture texture)
    {
        return texture.width ^ texture.height ^ texture.GetInstanceID();
    }
}
