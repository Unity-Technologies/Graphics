using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rendering = UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class LowResolutionRequester : MonoBehaviour
{
    public Material materialToRequest;
    List<int> properties = new List<int>();
    public int firstMipToRequest;

    // Start is called before the first frame update
    void Start()
    {
        if (materialToRequest == null) return;
        Shader shad = materialToRequest.shader;

        // Hack, we simply request all stacks in the shader ideally we would have some way
        // to identify a specific stack within a shader.
        properties.Clear();
        for ( int i=0;i<shad.GetPropertyCount(); i++)
        {
            if (shad.GetPropertyType(i) != UnityEngine.Rendering.ShaderPropertyType.Texture) continue;

            string name;
            int layer;

            if ( shad.FindTextureStack(i, out name, out layer))
            {
                int stackPropertyId = Shader.PropertyToID(name);
                if ( !properties.Contains(stackPropertyId))
                {
                    properties.Add(stackPropertyId);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
#if ENABLE_VIRTUALTEXTURES
        if (materialToRequest != null)
        {
            foreach (int prop in properties)
            {
                UnityEngine.Rendering.VirtualTexturing.Streaming.RequestRegion(materialToRequest, prop, new Rect(0.0f, 0.0f, 1.0f, 1.0f), firstMipToRequest, UnityEngine.Rendering.VirtualTexturing.System.AllMips);
            }
        }
#endif
    }
}
