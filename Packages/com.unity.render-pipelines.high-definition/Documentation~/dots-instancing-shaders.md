---
uid: um-dots-instancing-shaders
---

# DOTS Instancing shaders

To render large instance counts efficiently, BRG uses a new shader instancing mode called DOTS Instancing. Every shader that BRG uses must support DOTS Instancing. In traditional instanced shaders, the shader is passed an array for each instanced property in a constant or uniform buffer, such that each element in each array contains the property value for a single instance in the draw. In DOTS Instanced shaders, Unity passes one 32-bit integer to the shader for each DOTS Instanced property. This 32-bit integer is called a metadata value. This integer can represent anything you want, but typically it represents an offset in the buffer from where the shader loads property data for the instance that the shader is rendering.

DOTS Instancing has many advantages compared to traditional instancing, including the following:

* The instance data is stored in a GraphicsBuffer and remains persistent on the GPU, which means that Unity doesn't need to set it up again each time it renders the instance. Setting up data only when an instance actually changes can significantly improve performance in cases where instance data changes rarely or not at all. This is much more efficient than traditional instancing, which requires an engine to set up all data for every instance every frame.
* The process for setting up instance data is separate from setting up the draw call. This makes draw call setup lightweight and efficient. BRG makes this possible with a special fast path of the SRP Batcher that only does a minimal amount of work for each draw call. The responsibility for this work moves to you and gives you more control over what to render in each draw call.
* The size of a draw call is no longer limited by how much instance data can fit in a constant or uniform buffer. This makes it possible for BRG to render larger instance counts with a single draw call. 
  <br/>**Note**: The number of instance indices still limits the draw call size, since each index still requires some data. However, an index consumes far less memory than a full set of instanced properties which means many more instances can fit inside a constant or uniform buffer. For example, each index requires 16 byes so if the memory limit for a buffer on a particular platform is 64kb, 4096 indices can fit in the buffer. 
* If every instance uses the same value for a given property, it is possible to have all instances load the value from the same place in memory. This saves memory and the number of GPU cycles spent duplicating the value for each instance.



