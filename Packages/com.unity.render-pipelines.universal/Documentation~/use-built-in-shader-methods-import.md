# Import a file from the URP shader library

The High-Level Shader Language (HLSL) shader files for the Universal Render Pipeline (URP) are in the `Packages/com.unity.render-pipelines.universal/ShaderLibrary/` folder in your project.

To import a shader file into a custom shader file, add an `#include` directive inside the `HLSLPROGRAM` in your shader file. For example:

```hlsl
HLSLPROGRAM
...
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
...
ENDHLSL
```

You can then use the helper methods from the file. For example:

```hlsl
float3 cameraPosition = GetCameraPositionWS();
```

Refer to [Shader methods in URP](use-built-in-shader-methods.md) for more information about the different shader files.

You can also import shader files from the core Scriptable Render Pipeline (SRP). Refer to [Shader methods in Scriptable Render Pipeline (SRP) Core](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/manual/built-in-shader-methods.html).

## Examples

Refer to [Writing custom shaders](writing-custom-shaders-urp.md) for examples of using variables and helper methods from the files in the URP shader library.

## Additional resources

- [include and include_with_pragmas directives in HLSL](https://docs.unity3d.com/Manual/shader-include-directives.html)
