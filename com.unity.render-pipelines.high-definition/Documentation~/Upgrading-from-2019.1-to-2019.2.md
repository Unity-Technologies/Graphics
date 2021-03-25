# Upgrading HDRP from 5.x to Unity 6.x

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from Unity 5.x to 6.x.

## Custom Shaders

In 6.x, improvements to the Shader code for the HDRP Material framework give you more freedom when authoring Shaders. If you have created a custom Shader and want to upgrade your HDRP project to Unity 6.x, you must update the Shader to adhere to the new framework.

- In 6.x, HDRP's lighting model uses  `EvaluateBSDF()`, whereas 5.x uses `BSDF()`. `EvaluateBSDF()` returns a `CBSDF` structure which contains lighting information for reflected and transmitted diffuse and specular light. Before 6.x, HDRP applied the Lambert cosine automatically. From 6.x, you must apply it manually inside `EvaluateBSDF()`. Use an `IsNonZeroBSDF` to check whether you need to evaluate the BSDF.
- In 6.x, the  `GetBSDFAngles` function  accepts a different set of parameters as previously. It also no longer returns `NdotV` and `NdotV`, which you must now manually [clamp](<https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html>) outside of the function.
- In 6.x, the LightEvaluation and SurfaceShading files  include helper functions that you can choose to include or exclude in your Unity Project. The helper functions handle generic Material and light evaluation code that all HDRP Materials call (including StackLit and [Lit](Lit-Shader.md)). Use `#define` to include or remove each helper function.
- In 6.x, the `GetAmbientOcclusionForMicroShadowing()` method replaces `ComputeMicroShadowing()`. `GetAmbientOcclusionForMicroShadowing()` uses the ambient occlusion property for micro shadowing, whereas ComputeMicroShadowing() used the [Micro Shadow](Override-Micro-Shadows.md) value.
- WorldToTangent is now TangentToWorld because it was named incorrectly.

## ShaderGraphs

In 6.x, HDRP stores properties like SurfaceType, BlendMode, and DoubleSided inside the Material rather than the ShaderGraph. Every Material that uses properties like this may now display incorrectly in the Scene because their values might not match those in the [Master Node settings](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Master-Node.html). This change is relevant to every ShaderGraph Master Node, except for the [PBR](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/PBR-Master-Node.html) and [Unlit](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Unlit-Master-Node.html) cross-pipeline Master Nodes.

When you change these properties on the Material, HDRP toggles local Shader keywords in the generated Shader. The Master Node itself still displays the properties in the Settings View. The Master Nodes still store these property values, but they now serve as the default values for the Shader and for the Material. Property values on the Material override these default values.

This change means that HDRP needs to generate fewer Shader variants because it now relies on Shader keywords for render state control. This change also allows HDRP to process Shader stripping more efficiently.

### Upgrading

To upgrade all the ShaderGraphs in your Project, select **Edit > Render Pipelines > Reset All Shader Graphs Scene Material Properties (Project)**. This automatically:

1. Iterates over every Material in the Project.
2. Copies all the keywords needed to sync Material properties with their Master Node values.
3. Calls the HDRP material keyword reset function.
4. Sets the render queue of the Material to match the one on the ShaderGraph.

 To only upgrade the ShaderGraphs in your Scene, select **Edit > Render Pipelines > Reset All Shader Graphs Scene Material Properties (Scene)** instead.
