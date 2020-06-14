# Shader structure

This section provides an overview of a Unity shader structure.

Unity shader assets are written in a Unity-specific language called [ShaderLab](https://docs.unity3d.com/Manual/SL-Shader.html). 

A URP-compatible ShaderLab file contains some or all of the following blocks:
* [Shader](#shader)
* [Properties](#properties)
* [SubShader](#subshader)
* [Pass](#pass)
* [HLSLPROGRAM](#hlsl)
* [CBUFFER](#cbuffer)

<a name="shader"></a>

## Shader block

A ShaderLab file starts with the `Shader` declaration.

```c++
Shader "Example/URPUnlitShaderBasic"
```

The path in this declaration determines the location of the shader in the Shader menu on a Material.
![location of the shader in the Shader menu on a Material](Images/shader-examples/urp-material-ui-shader-path.png)

<a name="properties"></a>

## Properties block

This block contains Shader properties that you can access in the Inspector window on a Material.

```c++
Properties
{ 
    _BaseMap("Texture", 2D) = "white" {}
    _BaseColor("Color", Color) = (1, 1, 1, 1)
}
```

For more information on the Properties block, see the the page [ShaderLab: Properties](https://docs.unity3d.com/Manual/SL-Properties.html).

<a name="subshader"></a>

## SubShader block

A ShaderLab file contains one or more SubShader blocks. When rendering a mesh, Unity selects the first SubShader block that is compatible with the GPU on the target device.

For more information on the SubShader block, see the page [ShaderLab: SubShader](https://docs.unity3d.com/Manual/SL-SubShader.html).

A SubShader block contains the __Tags__ element and the [__Pass__](#pass) block.

Tags define when and under which conditions a SubShader block is executed. For more information on Tags, see [ShaderLab: SubShader Tags](https://docs.unity3d.com/Manual/SL-SubShaderTags.html).

<a name="pass"></a>

## Pass block

A Pass can contain information about the Pass itself (Pass name, Pass tags, etc.), and the HLSL program code. For more information, see [ShaderLab: Pass](https://docs.unity3d.com/Manual/SL-Pass.html).


<a name="hlsl"></a>

## HLSLPROGRAM block

This block contains the HLSL program code.

SRP shaders support only the HLSL language.

<a name="cbuffer"></a>

## CBUFFER block

In this block, you declare the variables that must be in the constant buffer.

### SRP Batcher compatibility

To ensure that a Shader is SRP Batcher compatible:
* Declare all Material properties in a single CBUFFER called `UnityPerMaterial`.
* Declare all built-in engine properties, such as `unity_ObjectToWorld` or `unity_WorldTransformParams`, in a single CBUFFER called `UnityPerDraw`.

