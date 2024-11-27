# Access DOTS Instancing properties in a custom shader

To access DOTS Instanced properties, your shader can use one of the access macros that Unity provides. The access macros assume that instance data in `unity_DOTSInstanceData` uses the following layout:

* The 31 least significant bits of the metadata value contain the byte address of the first instance in the batch within the `unity_DOTSInstanceData` buffer.
* If the most significant bit of the metadata value is `0`, every instance uses the value from instance index zero. This means each instance loads directly from the byte address in the metadata value. In this case, the buffer only needs to store a single value, instead of one value per instance.
* If the most significant bit of the metadata value is `1`, the address should contain an array where you can find the value for instance index `instanceID` using `AddressOfInstance0 + sizeof(PropertyType) * instanceID`. In this case, you should ensure that every rendered instance index has valid data in buffer. Otherwise, out-of-bounds access and undefined behavior can occur.

You can also set the metadata value directly which is useful if you want to use a custom data source that doesn't use the above layout, such as a texture.

For an example of how to use these macros, see [Access macro example](dots-instancing-shaders-samples.md).
