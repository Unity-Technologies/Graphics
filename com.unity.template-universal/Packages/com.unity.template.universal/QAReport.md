# Quality Report
Use this file to outline the test strategy for this package.

## QA Owner: Wyatt Sanders ( @wyatt )
## UX Owner: [*Add Name*]

## Test strategy
*Use this section to describe how this feature was tested.*
* A link to the Test Plan https://docs.google.com/document/d/1Ug9svCP4e9o3D55O3z-EUW3OMd_b5Z0WYPcZNGYYvn0/edit

### Default Scene Setup
- New project launched from Hub should load into SampleScene
- Scene View Mode = 3D
- Main Camera
  - Clear Flags = SKYBOX
  - Projection = PERSPECTIVE
  - MSAA = OFF
  - Occlusion Culling = ON
  - HDR = OFF
  - Should have camera WASD movement script attached
- Directional Light
  - Light color = ( R: 255, G: 244, B: 214 )
  - Intensity = 1
  - Indirect = 1
- Reflection Probes
  - Type = BAKED

### PostProcessing
- Should be included in the project
- Should be enabled and attached to the Main Camera in Sample Scene
- Should use PostProcessingProfile in Assets/Settings
- PostProcessingLayer on Camera
  - FXAA ( Fast Mode )
- PostProcessingVolume GameObject
 - Is Global = TRUE
 - Weight = 1
 - Priority = 0
 - Uses PostProcessingProfile asset found in Assets/Settings

### Lighting Settings
- Skybox = Procedural Unity Skybox in Assets/Materials
- Sun Source = In-Scene Directional Light
- Environment Lighting Settings
  - Source = SKYBOX
  - Intensity = 1
- Mixed Lighting Settings
  - Lighting Mode = Subtractive
- Environment Reflections
  - Defaults
- Precomputed Realtime GI = OFF
- Baked GI = ON
- Lightmapper
  - Type = PROGRESSIVE
- Auto-build lighting = ON

### Graphics Settings
- LightweightRenderPipeline Asset is set as active Graphics profile in Graphics Settings

### Player Settings
- Color space = LINEAR
- GPU Skinning = ON
- Optimize Mesh Data = ON
- Graphics Jobs = ON
- Dynamic Batching = OFF

### Time Settings
- Fixed Timestep = .2
- Max Allowed Timestep = .1

### Editor Settings
- Asset Serialization = FORCE TEXT
- Sprite Packer = OFF

## Package Status
Use this section to describe:
* UX status/evaluation results
* package stability
* known bugs, issues
* performance metrics,
* etc

