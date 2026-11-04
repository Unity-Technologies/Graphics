# Surface Cache

- The lighting rules in `../AGENTS.md` apply here. Surface Cache is the reference for them. Its `.hlsl` files declare no globals, and every pass in `SurfaceCache.cs` disables the keywords it enabled.
- Transparency mode is always one of two keywords, `OpaquesOnly` or `Cutouts`.
- Runtime tests live in `Tests/SRPTests/Packages/com.unity.testing.surface-cache.core/` and `Tests/SRPTests/Packages/com.unity.testing.surface-cache.urp/`. The URP_GI jobs run them through `Tests/SRPTests/Projects/UniversalGraphicsTest_Lighting`. Do not add runtime tests under `Packages/com.unity.render-pipelines.core/Tests/`.
