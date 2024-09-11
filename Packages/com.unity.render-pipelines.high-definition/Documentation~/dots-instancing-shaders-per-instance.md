# Example of a DOTS Instancing shader that accesses per-instance data

In this example:

* The metadata value for `Color` is `0x80001000`.
* The instance index is `5`.
* Data for instance 0 starts at address 0x1000.
* Data for instance 5 is at address 0x1000 + 5 * sizeof(float4) = 0x1050

Because the most significant bit is already set, the accessor macros don't load defaults. This means that `c0`, `c1`, and `c2` will all have the same value, loaded from `unity_DOTSInstanceData` address `0x1050`.

```
void ExamplePerInstance()
{
    // rawMetadataValue will contain 0x80001000
    uint rawMetadataValue = UNITY_DOTS_INSTANCED_METADATA_NAME(float4, Color);

    float4 c0 = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, Color);
    float4 c1 = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, Color);
    float4 c2 = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_CUSTOM_DEFAULT(float4, Color, float4(1, 2, 3, 4));
}
```
