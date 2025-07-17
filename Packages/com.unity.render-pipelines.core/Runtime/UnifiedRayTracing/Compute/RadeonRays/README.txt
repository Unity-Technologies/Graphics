This code has been ported from C++ to C# from the RadeonRays library. Version used: 4.1 release (https://github.com/GPUOpen-LibrariesAndSDKs/RadeonRays_SDK)

The files follow the same structure and naming as their original counterpart in https://github.com/GPUOpen-LibrariesAndSDKs/RadeonRays_SDK/tree/master/src/core/src/dx.
- Modifications have been done to the HLSL shaders to work around bugs found in the old FXC compiler.
- See comments in HlbvhBuilder.cs for modifications done to code responsible for the BVH build.