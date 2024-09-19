# Support DOTS Instancing in a a custom shader

To support DOTS Instancing, a shader needs to do the following:

* Use shader model 4.5 or newer. Specify `#pragma target 4.5` or higher.
* Support the `DOTS_INSTANCING_ON` keyword. Declare this with `#pragma multi_compile _ DOTS_INSTANCING_ON`.
* Declare at least one block of DOTS Instanced properties each of which has least one property. For more information, see [Declaring DOTS Instanced properties](dots-instancing-shaders-declare.md).

**Note**: Shader Graphs and shaders that Unity provides in URP and HDRP support DOTS Instancing.
