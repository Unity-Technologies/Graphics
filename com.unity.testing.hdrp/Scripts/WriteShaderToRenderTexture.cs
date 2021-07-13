using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WriteShaderToRenderTexture : MonoBehaviour
{
    [SerializeField] RenderTexture rt = null;
    [SerializeField] Shader shader = null;

    // Use this for initialization
    void Start()
    {
        Blit();
    }

    [ContextMenu("Update RenderTexture")]
    void Blit()
    {
        if ((rt == null) || (shader == null)) return;

        Material mat = new Material(shader);

        Graphics.Blit(null, rt, mat);
    }
}
