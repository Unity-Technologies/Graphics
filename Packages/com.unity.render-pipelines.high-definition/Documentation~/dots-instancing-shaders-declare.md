# Declare DOTS Instancing properties in a custom shader

To load instance data, such as transform matrices, the shader needs to define DOTS Instanced properties. Below is an example of a simple DOTS Instanced property block:

```
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, Color)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
```

To mark the beginning and end of the property block, use the `UNITY_DOTS_INSTANCING_START` and `UNITY_DOTS_INSTANCING_END` macros followed by the name of the block. The example uses the name `MaterialPropertyMetadata`. There are three allowed block names:

* BuiltinPropertyMetadata
* MaterialPropertyMetadata
* UserPropertyMetadata

The shader can declare one of each, so a DOTS Instanced shader can have between zero and three of such blocks. Unity-defined shader code doesn't use UserPropertyMetadata so this name is guaranteed to be free for you to use. URP and HDRP define BuiltinPropertyMetadata for every shader they provide and define MaterialPropertyMetadata for most of them too, so it's best practice to use UserPropertyMetadata. Your custom shaders can use all three possible names, even all at once.

The block can contain any number of DOTS Instanced property definitions formatted like:

```
UNITY_DOTS_INSTANCED_PROP(PropertyType, PropertyName)
```

`PropertyType` can be any HLSL built-in type (like uint, float4, float4x4, or int2x4) except a bool vector, and `PropertyName` is the name of the DOTS Instanced property. DOTS Instanced properties are completely separate from [regular material properties](https://docs.unity3d.com/6000.0/Documentation/Manual/SL-Properties), and you can give them the same name as another regular material property. This is possible because the `UNITY_DOTS_INSTANCED_PROP` macro generates special constant names which Unity recognizes that don't conflict with other property names. Shaders that Unity provides give DOTS Instanced properties the same names as regular material properties, but you don't need to follow this convention.

Internally, Unity provides the shader with a 32-bit integer metadata value for every DOTS Instanced property the shader declares. Unity sets the metadata value when your code makes a [BatchRendererGroup.AddBatch](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchRendererGroup.AddBatch) call to create the batch associated with the draw. The metadata value defaults to `0` if Unity doesn't set it. The shader also has access to `ByteAddressBuffer unity_DOTSInstanceData` which Unity sets to the GraphicsBuffer you pass as an argument to `BatchRendererGroup.AddBatch`. This buffer is typically where the shader loads the instance data from. Multiple batches can share a single GraphicsBuffer, but it is also possible for each batch to use its own separate GraphicsBuffer for `unity_DOTSInstanceData`.

**Note**: Unity doesn't provide any DOTS Instanced data automatically. It's your responsibility to make sure that the `unity_DOTSInstanceData` buffer of each batch contains the correct data. Instance data must include many properties that are Unity normally provides for GameObjects, such as transform matrices, light probe coefficients, and lightmap texture coordinates.
