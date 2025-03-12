# Execution order reference

The High Definition Render Pipeline (HDRP) executes render passes using the following stages:

1. Prepass
2. Prepare LightLoop data
3. Opaque and sky
4. Transparent
5. Post-processing
6. Overlay gizmos

## Prepass stage

HDRP executes the prepass stage in the following order:

1. Before Rendering - custom pass injection point
2. Prepass - depth, normal/smoothness, motion vectors
3. PreRenderSky/Cloud
4. DBuffer - no emissive decal
5. GBuffer
6. After Depth and Normal - custom pass injection point
7. Depth pyramid
8. Camera motion vectors

## Prepare Lightloop data stage

1. Shadows or cached shadows, or an [optional async pass](#optional-async-stage)
2. Screen space shadows
3. Generate MaxZPass

<a name="optional-async-stage"></a>
### Optional async pass

The optional async pass calculates the following:

- Lights list
- Screen space ambient occlusion (SSAO)
- Contact shadows
- Volume voxelization
- Screen space reflections (SSR)
- Screen space global illumination (SSGI)

### Opaque and sky stage

HDRP executes the opaque and sky stage in the following order:

1. Deferred lighting
2. Forward opaques
3. Decal emissive
4. Subsurface scattering
5. Sky
6. After Opaque and Sky - custom pass injection point
7. After Opaque and Sky - custom post-processing injection point

### Transparents stage

HDRP executes the transparents stage in the following order:

1. Clear stencil
2. Pre-refraction transparent depth prepass
3. Water G-buffer
4. Waterline
5. Transparent depth prepass
6. Volumetric lighting
7. Fog
8. Clouds
9. High Quality Line Rendering
10. Screen space reflections (SSR)
11. Before PreRefraction - custom pass injection point
12. Transparent pre-refraction
13. Water lighting
14. Color pyramid pre-refraction
15. Screen space multiple scattering (SSMS)
16. Before Transparent - custom pass injection point
17. Transparents
18. Low-resolution transparents
19. Combine transparents
20. Transparent PostPass
21. Transparent UI: HDR output

## Post-processing stage

HDRP executes the post-processing stage in the following order:

1. Color pyramid distortion
2. Distortion
3. Before Post Process - custom pass injection point
4. After Post Process Objects
5. Exposure
6. Deep learning super sampling (DLSS) upsample - before Post
7. Before Temporal anti-aliasing (TAA) - custom post-processing injection point
8. TAA or subpixel morphological anti-aliasing (SMAA)
9. Before Postprocess - custom post-processing injection point
10. Depth of field
11. DLSS upsample - After depth of field
12. Motion blur
13. After Post Process Blurs - custom post-processing injection point
14. Panini projection
15. Lens Flare (SRP)
16. Bloom
17. Uber pass - color grading, lens distortion, chromatic effect, vignette, composite
18. After Post Process - custom post-processing injection point
19. DLSS upsample - After Post
20. Final - grain, dithering, composite, fast approximate anti-aliasing (FXAA)
21. After Post Process - custom post-processing injection point

## Post-processing effect execution order

The post-processing system in HDRP applies post-processing effects in a specific order. The system also combines some effects into the same compute shader stack to minimize the number of passes.

HDRP executes post processing effects in the following order, from top to bottom.

| **Effect** | **Execution order** |
|-|-|
| NaN remover | Post-processing pass |
| Anti-aliasing - TAA, SMAA | Post-processing pass |
| Depth of field | Post-processing pass |
| Motion blur | Post-processing pass |
| Panini projection | Post-processing pass | 
| Bloom - pyramid | Post-processing pass |
| Color grading - look up table (LUT) baking | Post-processing pass |  
| Screen space lens flare, written in bloom texture | Post-processing pass  | 
| Lens flare (SRP) | Post-processing pass |
| Lens distortion | Compute shader stack |
| Chromatic aberration | Compute shader stack | 
| Apply bloom | Compute shader stack |
| Vignette | Compute shader stack | 
| Apply color grading | Compute shader stack | 
| Anti-aliasing - FXAA | Final post-processing pass |
| Film grain | Final post-processing pass | 
| 8-bit dithering | Final post-processing pass |

## Additional resources

- [Injection points](Custom-Pass-Injection-Points.md)
