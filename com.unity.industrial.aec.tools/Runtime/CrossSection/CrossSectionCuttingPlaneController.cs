using UnityEngine;
using UnityEngine.Experimental.Rendering;

#if UNITY_2018_3_OR_NEWER
[ExecuteAlways]
#else
[ExecuteInEditMode]
#endif
[DisallowMultipleComponent]
[AddComponentMenu("Rendering/Cross Section Cutting Plane Driver")]
public class CrossSectionCuttingPlaneController : MonoBehaviour
{
    public CrossSectionDefinitions.CutMainStyle cutMainStyle = CrossSectionDefinitions.CutMainStyle.Filled;

    void Update()
    {
        Shader.SetGlobalVector(CrossSectionDefinitions.ShaderPropertyIds.ClipPlanePosition, transform.position);
        Shader.SetGlobalVector(CrossSectionDefinitions.ShaderPropertyIds.ClipPlaneNormal, -transform.forward);
        Shader.SetGlobalVector(CrossSectionDefinitions.ShaderPropertyIds.ClipPlaneTangent, transform.right);
        Shader.SetGlobalVector(CrossSectionDefinitions.ShaderPropertyIds.ClipPlaneBitangent, transform.up);
        Shader.SetGlobalFloat(CrossSectionDefinitions.ShaderPropertyIds.CutMainStyle, (float)cutMainStyle);
    }
}
