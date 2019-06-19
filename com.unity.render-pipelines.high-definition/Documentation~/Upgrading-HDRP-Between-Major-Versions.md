# Upgrading between major Unity versions

In High Definition Render Pipeline (HDRP), some features work differently between major versions of Unity. This document helps you upgrade from one major version of Unity to another.

## Upgrading from Unity 2019.1 to Unity 2019.2

### Custom Shaders

In 2019.2, improvements to the Shader code for the HDRP Material framework give you more freedom when authoring Shaders. If you have created a custom Shader and want to upgrade your HDRP project to Unity 2019.2, you must update the Shader to adhere to the new framework.

- HDRP's lighting model uses  `EvaluateBSDF()` in 2019.2, whereas 2019.1 uses `BSDF()`. `EvaluateBSDF()` returns a `CBSDF` structure which contains lighting information for diffuse light reflected, diffuse light transmitted, specular light reflected, and specular light transmitted. Before 2019.2, HDRP applied the Lambert cosine automatically. From 2019.2, you must apply it manually inside `EvaluateBSDF()`. Use an `IsNonZeroBSDF` to check whether you need to evaluate the BSDF.
- The  `GetBSDFAngles` function does not take the same set of parameters as before. It also no longer returns `NdotV` and `NdotV`, which you must now manually clamp outside of the function.
- The LightEvaluation and SurfaceShading files files now include helper functions that you can choose to include or exclude in your Unity Project. The helper functions handle generic Material and light evaluation code that all HDRP Materials call (including StackLit and [Lit](Lit-Shader.html)). Use `#define` to include or remove each helper function.
- In 2019.2, the `GetAmbientOcclusionForMicroShadowing()` function replaces `ComputeMicroShadowing()`. `GetAmbientOcclusionForMicroShadowing()` uses the ambient occlusion property for micro shadowing, whereas ComputeMicroShadowing() used the [Micro Shadow](Override-Micro-Shadows.html) value.
- WorldToTangent is now TangentToWorld because it was originally named incorrectly.

### ShaderGraphs

In 2019.2, HDRP stores properties inside the Material rather than being hardcoded in the ShaderGraph. These properties include SurfaceType, BlendMode, and DoubleSided. This means that every Material that uses these properties may now display incorrectly because their values might not match those in the Master Node settings. This change is relevant to every ShaderGraph Master Node, except for the PBR and Unlit cross-pipeline Master Nodes.

When you change these properties on the Material, HDRP toggles local Shader keywords in the generated Shader. The Master Node itself still displays the properties in the Settings View. The Master Nodes still store these property values, but they now serve as the default values for the .shader and for the Material. Property values on the Material override these default values.

This change means that HDRP needs to generate fewer Shader variants because it now relies on Shader keywords for render state control. This change also allows HDRP to process Shader stripping more efficiently.

#### Upgrading

To upgrade ShaderGraphs automatically, select **Edit > Render Pipelines > Reset All Shader Graphs Scene Material Properties (Project)**. This automatically:

1. Iterates over every Material loaded in the Project.
2. Copies all the keywords needed to sync Material properties with their Master Node values.
3. Calls the HDRP material keyword reset function.
4. Sets the render queue of the Material to match the one on the ShaderGraph.

Note: If you just want to upgrade the ShaderGraphs in your Scene, select **Edit > Render Pipelines > Reset All Shader Graphs Scene Material Properties (Scene)** instead.