# Shader Core


- [Properties](#properties)

Here's a bunch of the more fleshed out core spec types


```
// Simple helper to represent having two names on most everything
class ShaderName
{
    string displayName;
    string referenceName;
}
```

```
// Variables can be represented all over the place
class ShaderVariable
{
    ShaderType shaderType;
    ShaderName name;
    // Do we need some high level attributes / metadata too?
}
```
```
// Variables can be represented all over the place
class ShaderType
{
    ShaderName name;
    List<ShaderVariable> fields;
    // This is very high level for simplicity, the sandbox has a more robust version of this
    BaseType {Scalar, Vector, Matrix, Struct, ...}
}
```
```
// A parameters need to know the in/out mode
class ShaderParameter : ShaderVariable
{
    ParameterFlags flags {None, Input, Output}
}
```
```
class ShaderFunction
{
    ShaderName name;
    List<ShaderParameter> parameters;
    ShaderType returnType;
    // Probably also includes:
    Includes, body, dependencies (functions called)
}
```
```
// A property is a variable with a bit more information, mainly about how it's exposed
class ShaderProperty : ShaderVariable
{
    PropertyType flags{None, Input, Output, Property, Required}
    Attributes?
}
```

---
# Properties

Properties are actually significantly more complicated than listed above, due to the three different places a property is declared as well as hidden properties. For example, declaring the material property:
```
_Texture2D("Texture2D", 2D) = "white" {}
```

implicitly creates 4 properties in the shader:
```
CBUFFER_START(UnityPerMaterial)
float4 _Texture2D_ST;
float4 _Texture2D_TexelSize;
CBUFFER_END
// Object and Global properties
TEXTURE2D(_Texture2D);
SAMPLER(sampler_Texture2D);
```
and then there's the question of what does the block take as inputs and declare as properties? Does a block have:
```
Input {        
    float4 _Texture2D_ST;
    float4 _Texture2D_TexelSize;

    Texture2D _Texture2D;
    Sampler sampler_Texture2D;
}
```

or does it declare:
```
Input {        
    UnityTexture2d _Texture2D;
}
```

There's also fun interesting issues like Color being the material property type but float4 being the shader type. Internally this means that properties roughly have to be represented with this data:
```
internal class ShaderMaterialPropertyInfo
{
    internal string referenceName = "";
    internal string displayName = "";
    internal string propertyType = "";
    internal string defaultValue = "";
    internal List<ShaderAttribute> Attributes = new List<ShaderAttribute>();
}

internal class ShaderPassPropertyInfo
{
    internal string referenceName = "";
    internal string propertyType = "";
    internal List<ShaderAttribute> Attributes = new List<ShaderAttribute>();
    /// Is this declared in global scope? Per material? Hybrid? No declared?
    internal HLSLDeclaration declarationType;
}

internal class ShaderProperty : ShaderVariable
{
    internal enum PropertyType {None, Input, Output, InputOutput, Property}
    internal PropertyType PropType = PropertyType.None;
    internal List<ShaderMaterialPropertyInfo> materialProps = new List<ShaderMaterialPropertyInfo>();
    internal List<ShaderPassPropertyInfo> passProps = new List<ShaderPassPropertyInfo>();
}
```
as a not only is there not a 1-1 mapping of material/shader/block properties, but their reference names (e.g. "_Texture2D_ST"), types, default values, etc... may be very very different.

As far as the block syntax. I think it makes sense to have blocks take the underlying shader type. We can use the combined unity types if we want as well to use when we want the packed data. Various attribute tags can be added to control what hidden properties are declared too.
```
Input {        
    // Just declares the material and shader properties the same (via extra replacement necessary).
    // Additionally prob needs syntax for defaults
    Texture2D _Texture2D;
    // Declares all 4 properties in the shader and a "Texture 2D" for the material.
    // UnityTexture2D will by dynamically constructed from the individual fields and
    // passed into the block's entry point function so the user doesn't have to know about the details.
    UnityTexture2D _CombinedTexture;
    // Same as above, but the '_ST' shader property isn't declared.
    [NoScaleOffset] UnityTexture2D _CombinedTextureNoScaleOffset;
    // Declares a float4 shader property but a color material property.
    // Optionally we could actually declare the type as Color and then do a replacement in the code body or even just declare a typedef in hlsl.
    [Color] float4 _MyColor;

    // Additionally extra attributes could be used to control the HLSLDeclaration type above, especially [Hidden].
}
```