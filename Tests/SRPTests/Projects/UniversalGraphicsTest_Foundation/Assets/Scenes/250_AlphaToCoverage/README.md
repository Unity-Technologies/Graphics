# Alpha-To-Coverage Test

This goal of this test is to verify that alpha-to-coverage (aka AlphaToMask) is working correctly in URP for both standard **AND** shader graph shaders. Since alpha-to-coverage is based on MSAA, there are two separate scenes for this test. One that disables MSAA and one that uses whatever MSAA configuration the URP pipeline asset has which is expected to be 4xMSAA.

The scene renders eight spheres in two rows of four. The front row is using standard materials and the back row is using equivalent shader graph materials.

From left to right, the test renders:

1. Opaque
2. Opaque & Alpha Clip
3. Transparent & Alpha Clip
4. Transparent

The only visible differences between the MSAA and non-MSAA scenes should be on the holes in the Opaque & Alpha Clip spheres. (and all geometry edges of course)

At the bottom of the test image, there's another row of four cubes. All are expected to be visible except the second from the left.
These primitives are testing the edge cases where alpha and cutoff are set to either exactly one or exactly zero.
These cases typically don't happen when alpha is being used in traditional ways, but are easy to trigger with editor UI sliders.

There's also a sphere that uses a negative alpha cutoff value in the place of the clipped cube second from the left.
This situation is possible to create via shader graph, so we have to test that it's handled properly.