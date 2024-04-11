# Use shader methods from the SRP Core shader library

SRP Core has a library of High-Level Shader Language (HLSL) shader files that contain helper methods. You can import these files into your custom shader files and use the helper methods.

To use the following methods, add `#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"` inside the `HLSLPROGRAM` in your shader file.

### Get matrices

| **Method** | **Syntax** | **Description** |
|-|-|-|
| `CreateTangentToWorld` | `real3x3 CreateTangentToWorld(real3 normal, real3 tangent, real flipSign)` | Returns the matrix that converts tangents to world space. |
| `GetObjectToWorldMatrix()` | `float4x4 GetObjectToWorldMatrix()` | Returns the matrix that converts positions in object space to world space. |
| `GetViewToHClipMatrix()` | `float4x4 GetViewToHClipMatrix()` | Returns the matrix that converts positions in view space to clip space. |
| `GetViewToWorldMatrix()` | `float4x4 GetViewToWorldMatrix()` | Returns the matrix that converts positions in view space to world space. |
| `GetWorldToHClipMatrix()` | `float4x4 GetWorldToHClipMatrix()` | Returns the matrix that converts positions in world space to clip space. |
| `GetWorldToObjectMatrix()` | `float4x4 GetWorldToObjectMatrix()` | Returns the matrix that converts positions in world space to object space. |
| `GetWorldToViewMatrix()` | `float4x4 GetWorldToViewMatrix()` | Returns the matrix that converts positions in world space to view space. |

### Transform positions

| **Method** | **Syntax** | **Description** |
|-|-|-|
| `TransformObjectToHClip` | `float4 TransformObjectToHClip(float3 positionInObjectSpace)` | Converts a position in object space to clip space. |
| `TransformObjectToWorld` | `float3 TransformObjectToWorld(float3 positionInObjectSpace)` | Converts a position in object space to world space. |
| `TransformViewToWorld` | `float3 TransformViewToWorld(float3 positionInViewSpace)` | Converts a position in view space to world space. |
| `TransformWorldToHClip` | `float4 TransformWorldToHClip(float3 positionInWorldSpace)` | Converts a position in world space to clip space. |
| `TransformWorldToObject` | `float3 TransformWorldToObject(float3 positionInWorldSpace)` | Converts a position in world space to object space. |
| `TransformWorldToView` | `float3 TransformWorldToView(float3 positionInWorldSpace)` | Converts a position in world space to view space. |
| `TransformWViewToHClip` | `float4 TransformWViewToHClip(float3 positionInViewSpace)` | Converts a position in view space to clip space. |

### Transform directions

| **Method** | **Syntax** | **Description** |
|-|-|-|
| `TransformObjectToTangent` | `real3 TransformObjectToTangent(real3 directionInObjectSpace, real3x3 tangentToWorldMatrix)` | Converts a direction in object space to tangent space, using a tangent-to-world matrix. |
| `TransformObjectToWorldDir` | `float3 TransformObjectToWorldDir(float3 directionInObjectSpace, bool normalize = true)` | Converts a direction in object space to world space. |
| `TransformTangentToObject` | `real3 TransformTangentToObject(real3 dirTS, real3x3 tangentToWorldMatrix)` | Converts a direction in tangent space to object space, using a tangent-to-world matrix. |
| `TransformTangentToWorldDir` | `real3 TransformTangentToWorldDir(real3 directionInWorldSpace, real3x3 tangentToWorldMatrix, bool normalize = false)` | Converts a direction in tangent space to world space, using a tangent-to-world matrix. |
| `TransformViewToWorldDir` | `real3 TransformViewToWorldDir(real3 directionInViewSpace, bool normalize = false)` | Converts a direction in view space to world space. |
| `TransformWorldToHClipDir` | `real3 TransformWorldToHClipDir(real3 directionInWorldSpace, bool normalize = false)` | Converts a direction in world space to clip space. |
| `TransformWorldToObjectDir` | `float3 TransformWorldToObjectDir(float3 directionInWorldSpace, bool normalize = true)` | Converts a direction in world space to object space. |
| `TransformWorldToTangentDir` | `real3 TransformWorldToTangentDir(real3 directionInWorldSpace, real3x3 tangentToWorldMatrix, bool normalize = false)` | Converts a direction in world space to tangent space, using a tangent-to-world matrix. |
| `TransformWorldToViewDir` | `real3 TransformWorldToViewDir(real3 directionInWorldSpace, bool normalize = false)` | Converts a direction in world space to view space. |

### Transform surface normals

| **Method** | **Syntax** | **Description** |
|-|-|-|
| `TransformObjectToWorldNormal` | `float3 TransformObjectToWorldNormal(float3 normalInObjctSpace, bool normalize = true)` | Converts a normal in object space to world space. |
| `TransformTangentToWorld` | `float3 TransformTangentToWorld(float3 normalInTangentSpace, real3x3 tangentToWorldMatrix, bool normalize = false)` | Converts a normal in tangent space to world space, using a tangent-to-world matrix. |
| `TransformViewToWorldNormal` | `real3 TransformViewToWorldNormal(real3 normalInViewSpace, bool normalize = false)` | Converts a normal in view space to world space. |
| `TransformWorldToObjectNormal` | `float3 TransformWorldToObjectNormal(float3 normalInWorldSpace, bool normalize = true)` | Converts a normal in world space to object space. |
| `TransformWorldToTangent` | `float3 TransformWorldToTangent(float3 normalInWorldSpace, real3x3 tangentToWorldMatrix, bool normalize = true)` | Converts a normal in world space to tangent space using a tangent-to-world matrix. |
| `TransformWorldToViewNormal` | `real3 TransformWorldToViewNormal(real3 normalInWorldSpace, bool normalize = false)` | Converts a normal in world space to view space. |

## Additional resources

- [HLSL in Unity](https://docs.unity3d.com/Manual/SL-ShaderPrograms.html)


