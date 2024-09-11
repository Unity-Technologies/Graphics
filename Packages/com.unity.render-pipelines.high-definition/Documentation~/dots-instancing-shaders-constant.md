# Example of a DOTS Instancing shader that accesses constant data

In this example:

* The metadata value for `Color` is `0x00001000`.
* The instance index is `5`.
* Data for instance 0 starts at address 0x1000.
* The most significant bit is not set so data for instance 5 is at the same address as instance 0.

Because the most significant bit is not set, the accessor macros that fall back to defaults don't access `unity_DOTSInstanceData`. This means that:

* `c0` will contain the value from `unity_DOTSInstanceData` address `0x1000`.
* `c1` will contain the value of the regular material property **Color**, and cause a compile error if the Color property doesn't exist.
* `c2` will contain `(1, 2, 3, 4)` because that was passed as the explicit default value.

```
void ExampleConstant()
{
    // rawMetadataValue will contain 0x00001000
    uint rawMetadataValue = UNITY_DOTS_INSTANCED_METADATA_NAME(float4, Color);
    float4 c0 = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, Color);
    float4 c1 = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, Color);
    float4 c2 = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_CUSTOM_DEFAULT(float4, Color, float4(1, 2, 3, 4));
}
```