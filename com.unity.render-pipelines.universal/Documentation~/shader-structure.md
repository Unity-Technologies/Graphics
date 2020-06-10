# Shader structure

This section provides an overview of a Unity shader structure.

Shader assets in Unity are written in a declarative language called [ShaderLab](https://docs.unity3d.com/Manual/SL-Shader.html). 

A URP-compatible ShaderLab file contains some or all of the following blocks:
* [Shader](#shader)
* [Properties](#properties)
* [SubShader](#subshader)
* [Pass]()
* [HLSLPROGRAM]()
* [CBUFFER]()

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

For more information on this block, see the the page [ShaderLab: Properties](https://docs.unity3d.com/Manual/SL-Properties.html).

<a name="subshader"></a>

## SubShader

A ShaderLab file contains one or more SubShader blocks. When rendering a mesh, Unity selects the first SubShader block that is compatible with the GPU on the target device.

TODO: START HERE.

(https://docs.unity3d.com/Manual/SL-SubShader.html)
