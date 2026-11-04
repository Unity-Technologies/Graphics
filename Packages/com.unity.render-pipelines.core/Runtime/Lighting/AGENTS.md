# Lighting code

These rules are the standard for new lighting code in this folder and in the folders that point to this file: `../PathTracing`, `../UnifiedRayTracing` and the URP Surface Cache GI renderer feature. Surface Cache (`SurfaceCache/`) follows them and is the code to copy. Older lighting code may not follow them.

Shader inputs and state:

- Only kernel, vertex and fragment entry points read shader globals. Library `.hlsl` files declare no globals. Their functions take inputs as parameters, grouped into a parameter struct when there are many (`PatchUtil::VolumeParamSet` in `SurfaceCache/PatchUtil.hlsl`).
- Do not pass a result to other code by writing a shader global.
- A pass that enables a keyword on a shared compute shader or material disables it again after its dispatch or draw, as the passes in `SurfaceCache/SurfaceCache.cs` do. A mode is always one keyword out of a complete set, never "no keyword".
- Restore a global flag such as `ShaderUtil.allowAsyncCompilation` before the function that changed it returns.

Shader structure:

- Each `.compute` file holds one kernel. Variants of that kernel for a ray tracing backend or for stereo rendering are acceptable (`SurfaceCache/Estimation.compute` has a compute and a hardware variant). Code that kernels share goes in an included `.hlsl` file.
- In `.hlsl` files, constants are `static const`, not `#define`. A `.compute` file uses `#define` only for values that `[numthreads]` needs.
- A macro that an including file defines before the `#include` is named after the file that reads it: `PATCH_UTIL_USE_RW_*` for `SurfaceCache/PatchUtil.hlsl`, `RING_BUFFER_USE_RW_RING_CONFIG_BUFFER` for `SurfaceCache/RingBuffer.hlsl`, `UNIFIED_RT_*` for Unified Ray Tracing.

Layering:

- Library code knows nothing about its callers: no camera, depth buffer, editor, play mode, URP, renderer feature or test concepts. It reads no `RenderSettings`, `SystemInfo`, `Time.frameCount` or render-pipeline state. The calling feature reads those and passes the values in.
- Work that needs a camera or screen pixels lives in the render pipeline feature. The Surface Cache screen resolve, denoising and upsampling passes are in the URP feature, not in `SurfaceCache/`.
- Callers pass every input explicitly. Do not add optional parameters with default values. A caller that wants the default configuration names it.

Failures:

- Return failures to the caller and let the calling feature log them. Do not add `Debug.Log`, `Debug.LogWarning` or `Debug.LogError` for a resource or capacity failure.
- Use `Debug.Assert` for states that cannot happen, and do not handle them gracefully. Use control flow only for states a caller can reach.
- A fallible function that produces an index or slot returns `bool` and gives the value through an `out` parameter, as `MaterialPool.TryAddCookieTexture` in `../PathTracing` does. Do not encode failure in a magic index such as `-1` or `int.MaxValue`. A typed handle with an `Invalid` value is acceptable.
- Report each failure one way only. Do not return a failure value and also throw or catch an exception for the same failure.
- Callers check a fallible result before they index or look up with it.

Naming:

- Give a type that groups inputs a `ParameterSet` suffix in C# (`SurfaceCacheVolumeParameterSet`) and a `ParamSet` suffix in HLSL (`VolumeParamSet`). Do not use a `Data` suffix, except for RenderGraph `PassData`.
- Call the material that renders an environment cubemap the "cubemap material" (`_cubemapMaterial` in `../PathTracing/Environment/CubemapRender.cs`), not the "sky box". Its shader may not render a sky. Unity API names such as `RenderSettings.skybox` keep their spelling.
