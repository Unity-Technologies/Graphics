# Alpha-To-Coverage Test

This goal of this test is to verify that alpha-to-coverage (aka AlphaToMask) is working correctly in URP for both standard **AND** shader graph shaders. Since alpha-to-coverage is based on MSAA, there are two separate scenes for this test. One that disables MSAA and one that uses whatever MSAA configuration the URP pipeline asset has which is expected to be 4xMSAA.

The scene renders eight spheres in two rows of four. The front row is using standard materials and the back row is using equivalent shader graph materials.

From left to right, the test renders:

1. Opaque
2. Opaque & Alpha Clip
3. Transparent & Alpha Clip
4. Transparent

The only visible differences between the MSAA and non-MSAA scenes should be on the holes in the Opaque & Alpha Clip spheres. (and all geometry edges of course)
