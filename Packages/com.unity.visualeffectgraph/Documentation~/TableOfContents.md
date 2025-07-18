* [Visual Effect Graph](index.md)
* [Requirements](System-Requirements.md)
* [What's new](whats-new.md)
  * [10 / Unity 2020.2](whats-new-10.md)
  * [11 / Unity 2021.1](whats-new-11.md)
  * [12 / Unity 2021.2](whats-new-12.md)
  * [13 / Unity 2022.1](whats-new-13.md)
  * [14 / Unity 2022.2](whats-new-14.md)
  * [15 / Unity 2023.1](whats-new-15.md)
  * [16 / Unity 2023.2](whats-new-16.md)
  * [17 / Unity 2023.3](whats-new-17.md)
  * [17.1 / Unity 6.1](whats-new-17-1.md)
* [Getting Started](GettingStarted.md)
  * [Visual Effect Graph Assets](VisualEffectGraphAsset.md)
  * [Visual Effect Graph Window](VisualEffectGraphWindow.md)
  * [Experimental Features](ExperimentalFeatures.md)
  * [Sample Content](sample-content.md)
    * [Learning Templates](sample-learningTemplates.md)
  * [Shortcuts](Shortcuts.md) 
* [Graph Logic & Philosophy](GraphLogicAndPhilosophy.md)
  * [Systems](Systems.md)
  * [Contexts](Contexts.md)
  * [Blocks](Blocks.md)
  * [Operators](Operators.md)
  * [Properties](Properties.md)
  * [Events](Events.md)
  * [Attributes](Attributes.md)
  * [Subgraph](Subgraph.md)
  * [Blackboard](Blackboard.md)
  * [Sticky Notes](StickyNotes.md)
  * [Templates Window](Templates-window.md)
  * [Project Settings](VisualEffectProjectSettings.md)
  * [Preferences](VisualEffectPreferences.md)
  * [Visual Effect Bounds](visual-effect-bounds.md)
  * [Custom HLSL](CustomHLSL-Common.md)
  * [Instancing](Instancing.md)
* [The Visual Effect Component](VisualEffectComponent.md)
  * [C# Component API](ComponentAPI.md)
  * [Using Visual Effects with Timeline](Timeline.md)
  * [Property Binders](PropertyBinders.md)
  * [Event Binders](EventBinders.md)
  * [Output Event Handlers](OutputEventHandlers.md)
* Shader Graph Integration
  * [Shader Graphs in Visual Effects](sg-working-with.md)
  * [Visual Effect Target](sg-target-visual-effect.md)
* Pipeline Tools
  * [Representing Complex Shapes](representing-complex-shapes.md)
    * [Signed Distance Fields](sdf-in-vfx-graph.md)
      * [SDF Bake Tool](sdf-bake-tool.md)
        * [SDF Bake Tool window](sdf-bake-tool-window.md)
        * [SDF Bake Tool API](sdf-bake-tool-api.md)
    * [Point Caches](point-cache-in-vfx-graph.md)
      * [Point Cache asset](point-cache-asset.md)
      * [Point Cache Bake Tool](point-cache-bake-tool.md)
  * [ExposedProperty Helper](ExposedPropertyHelper.md)
  * [Vector Fields](VectorFields.md)
  * [Spawner Callbacks](SpawnerCallbacks.md)
* [Node Library](node-library.md)
  * Context
    * [Event](Context-Event.md)
    * [GPU Event](Context-GPUEvent.md)
    * [Initialize Particle](Context-Initialize.md)
    * [Output Mesh](Context-OutputMesh.md)
    * [Output Distortion](Context-OutputDistortion.md)
    * [Output Decal](Context-OutputForwardDecal.md)
    * [Output Line](Context-OutputLine.md)
    * [Output Particle Mesh](Context-OutputParticleMesh.md)
    * [Output Particle HDRP Lit Decal](Context-OutputParticleHDRPLitDecal.md)
    * [Output Particle HDRP Volumetric Fog](Context-OutputParticleHDRPVolumetricFog.md)
    * [Output Particle URP Lit Decal](Context-OutputParticleURPLitDecal.md)
    * [Output Point](Context-OutputPoint.md)
    * [Output Primitive](Context-OutputPrimitive.md)
    * [Output ShaderGraph Quad](Context-OutputShaderGraphPlanarPrimitive.md)
    * [Ouput Particle ShaderGraph Mesh](Context-OutputShaderGraphMesh.md)
    * [Output ParticleStrip ShaderGraph Quad](Context-OutputShaderGraphStrip.md)
    * Shared Output Settings
      * [Global Settings](Context-OutputSharedSettings.md)
      * [Lit Output Settings](Context-OutputLitSettings.md)
    * [Spawn](Context-Spawn.md)
    * [Update Particle](Context-Update.md)
  * Block
    * Attribute
      * [Curve](Block-SetAttributeFromCurve.md)
      * Derived
        * [Calculate Mass from Volume](Block-CalculateMassFromVolume.md)
      * [Map](Block-SetAttributeFromMap.md)
      * [Set](Block-SetAttribute.md)
    * [Collision](Block-Collision-LandingPage.md)
      * [Collision Shape](Block-CollisionShape.md)
      * [Collision Depth Buffer](Block-CollideWithDepthBuffer.md)
      * [Kill Shape](Block-KillShape.md)
      * [Trigger Shape](Block-TriggerShape.md)
    * Flipbook
      * [Flipbook Player](Block-FlipbookPlayer.md)
    * Force
      * [Attractor Shape Signed Distance Field](Block-ConformToSignedDistanceField.md)
      * [Attractor Shape Sphere](Block-ConformToSphere.md)
      * [Force](Block-Force.md)
      * [Gravity](Block-Gravity.md)
      * [Linear Drag](Block-LinearDrag.md)
      * [Turbulence](Block-Turbulence.md)
      * [Vector Force Field](Block-VectorForceField.md)
    * HLSL
      * [Custom HLSL](Block-CustomHLSL.md)
    * Implicit
      * [Integration : Update Position](Block-UpdatePosition.md)
      * [Integration : Update Rotation](Block-UpdateRotation.md)
    * Orientation
      * [Connect Target](Block-ConnectTarget.md)
      * [Orient](Block-Orient.md)
    * Output
      * [Camera Fade](Block-CameraFade.md)
      * [Subpixel Anti-Aliasing](Block-SubpixelAntiAliasing.md)
    * [Position Shape](Block-SetPositionShape-LandingPage.md)
      * [Set Position (Depth)](Block-SetPosition(Depth).md)
      * [Set Position (Mesh)](Block-SetPosition(Mesh).md)
      * [Set Position (Skinned Mesh)](Block-SetPosition(SkinnedMesh).md)
      * [Set Position Shape](Block-SetPositionShape.md)
      * [Set Position (Sequential)](Block-SetPosition(Sequential).md)
      * [Tile/Warp Positions](Block-TileWarpPositions.md)
    * Size
      * [Screen Space Size](Block-ScreenSpaceSize.md)
    * Spawn
      * [Constant Spawn Rate](Block-ConstantRate.md)
      * [Periodic Burst](Block-Burst.md)
      * [Single  Burst](Block-Burst.md)
      * [Variable Spawn Rate](Block-VariableRate.md)
      * Attribute
        * [Set Spawn Event \<attribute>](Block-SetSpawnEvent.md)
      * Custom
        * [Increment Strip Index On Start](Block-IncrementStripIndexOnStart.md)
        * [Set Spawn Time](Block-SetSpawnTime.md)
        * [Spawn Over Distance](Block-SpawnOverDistance.md)
    * [Trigger Event Block reference](Block-Trigger-Event.md)
    * Velocity
      * [Velocity from Direction & Speed (Change Speed)](Block-VelocityFromDirectionAndSpeed(ChangeSpeed).md)
      * [Velocity from Direction & Speed (New Direction)](Block-VelocityFromDirectionAndSpeed(NewDirection).md)
      * [Velocity from Direction & Speed (Random Direction)](Block-VelocityFromDirectionAndSpeed(RandomDirection).md)
      * [Velocity from Direction & Speed (Spherical)](Block-VelocityFromDirectionAndSpeed(Spherical).md)
      * [Velocity from Direction & Speed (Tangent)](Block-VelocityFromDirectionAndSpeed(Tangent).md)
  * Operator
    * Attribute
      * [Age Over Lifetime](Operator-AgeOverLifetime.md)
      * [Get Attribute: age](Operator-GetAttributeAge.md)
      * [Get Attribute: alive](Operator-GetAttributeAlive.md)
      * [Get Attribute: alpha](Operator-GetAttributeAlpha.md)
      * [Get Attribute: angle](Operator-GetAttributeAngle.md)
      * [Get Attribute: angularVelocity](Operator-GetAttributeAngularVelocity.md)
      * [Get Attribute: axisX](Operator-GetAttributeAxisX.md)
      * [Get Attribute: axisY](Operator-GetAttributeAxisY.md)
      * [Get Attribute: axisZ](Operator-GetAttributeAxisZ.md)
      * [Get Attribute: color](Operator-GetAttributeColor.md)
      * [Get Attribute: direction](Operator-GetAttributeDirection.md)
      * [Get Attribute: lifetime](Operator-GetAttributeLifetime.md)
      * [Get Attribute: mass](Operator-GetAttributeMass.md)
      * [Get Attribute: oldPosition](Operator-GetAttributeOldPosition.md)
      * [Get Attribute: particleCountInStrip](Operator-GetAttributeParticleCountInStrip.md)
      * [Get Attribute: particleId](Operator-GetAttributeParticleID.md)
      * [Get Attribute: particleIndexInStrip](Operator-GetAttributeParticleIndexInStrip.md)
      * [Get Attribute: pivot](Operator-GetAttributePivot.md)
      * [Get Attribute: position](Operator-GetAttributePosition.md)
      * [Get Attribute: scale](Operator-GetAttributeScale.md)
      * [Get Attribute: seed](Operator-GetAttributeSeed.md)
      * [Get Attribute: size](Operator-GetAttributeSize.md)
      * [Get Attribute: spawnIndex](Operator-GetAttributeSpawnIndex.md)
      * [Get Attribute: spawnTime](Operator-GetAttributeSpawnTime.md)
      * [Get Attribute: stripIndex](Operator-GetAttributeStripIndex.md)
      * [Get Attribute: targetPosition](Operator-GetAttributeTargetPosition.md)
      * [Get Attribute: texIndex](Operator-GetAttributeTexIndex.md)
      * [Get Attribute: velocity](Operator-GetAttributeVelocity.md)
      * [Get Custom Attribute](Operator-GetCustomAttribute.md)
    * Bitwise
      * [And](Operator-BitwiseAnd.md)
      * [Complement](Operator-BitwiseComplement.md)
      * [Left Shift](Operator-BitwiseLeftShift.md)
      * [Or](Operator-BitwiseOr.md)
      * [Right Shift](Operator-BitwiseRightShift.md)
      * [Xor](Operator-BitwiseXor.md)
    * Builtin
      * [Delta Time](Operator-DeltaTime.md)
      * [Frame Index](Operator-FrameIndex.md)
      * [Local to World](Operator-LocalToWorld.md)
      * [Main Camera](Operator-MainCamera.md)
      * [System Seed](Operator-SystemSeed.md)
      * [Total Time](Operator-TotalTime.md)
      * [World to Local](Operator-WorldToLocal.md)
    * Camera
      * [Viewport to World Point](Operator-ViewportToWorldPoint.md)
      * [World to Viewport Point](Operator-WorldToViewportPoint.md)
    * Color
      * [Color Luma](Operator-ColorLuma.md)
      * [HSV to RGB](Operator-HSVToRGB.md)
      * [RBG to HSV](Operator-RGBToHSV.md)
    * HLSL
      * [Custom HLSL](Operator-CustomHLSL.md)
    * Inline
      * [AABox](Operator-InlineAABox.md)
      * [AnimationCurve](Operator-InlineAnimationCurve.md)
      * [ArcCircle](Operator-InlineArcCircle.md)
      * [ArcCone](Operator-InlineArcCone.md)
      * [ArcSphere](Operator-InlineArcSphere.md)
      * [ArcTorus](Operator-InlineArcTorus.md)
      * [bool](Operator-Inlinebool.md)
      * [Camera](Operator-InlineCamera.md)
      * [Circle](Operator-InlineCircle.md)
      * [Color](Operator-InlineColor.md)
      * [Cone](Operator-InlineCone.md)
      * [Cubemap](Operator-InlineCubemap.md)
      * [CubemapArray](Operator-InlineCubemapArray.md)
      * [Cylinder](Operator-InlineCylinder.md)
      * [Direction](Operator-InlineDirection.md)
      * [FlipBook](Operator-InlineFlipBook.md)
      * [float](Operator-Inlinefloat.md)
      * [Gradient](Operator-InlineGradient.md)
      * [int](Operator-Inlineint.md)
      * [Line](Operator-InlineLine.md)
      * [Matrix4x4](Operator-InlineMatrix4x4.md)
      * [Mesh](Operator-InlineMesh.md)
      * [OrientedBox](Operator-InlineOrientedBox.md)
      * [Plane](Operator-InlinePlane.md)
      * [Position](Operator-InlinePosition.md)
      * [Sphere](Operator-InlineSphere.md)
      * [TerrainType](Operator-InlineTerrainType.md)
      * [Texture2D](Operator-InlineTexture2D.md)
      * [Texture2DArray](Operator-InlineTexture2DArray.md)
      * [Texture3D](Operator-InlineTexture3D.md)
      * [Torus](Operator-InlineTorus.md)
      * [Transform](Operator-InlineTransform.md)
      * [uint](Operator-Inlineuint.md)
      * [Vector](Operator-InlineVector.md)
      * [Vector2](Operator-InlineVector2.md)
      * [Vector3](Operator-InlineVector3.md)
      * [Vector4](Operator-InlineVector4.md)
    * Logic
      * [And](Operator-LogicAnd.md)
      * [Branch](Operator-Branch.md)
      * [Compare](Operator-Compare.md)
      * [Nand](Operator-LogicNand.md)
      * [Nor](Operator-LogicNor.md)
      * [Not](Operator-LogicNot.md)
      * [Or](Operator-LogicOr.md)
      * [Switch](Operator-Switch.md)
    * Math
      * Arithmetic
        * [Absolute](Operator-Absolute.md)
        * [Add](Operator-Add.md)
        * [Divide](Operator-Divide.md)
        * [Fractional](Operator-Fractional.md)
        * [Inverse Lerp](Operator-InverseLerp.md)
        * [Lerp](Operator-Lerp.md)
        * [Modulo](Operator-Modulo.md)
        * [Multiply](Operator-Multiply.md)
        * [Negate](Operator-Negate.md)
        * [One Minus](Operator-OneMinus.md)
        * [Power](Operator-Power.md)
        * [Reciprocal](Operator-Reciprocal.md)
        * [Sign](Operator-Sign.md)
        * [Smoothstep](Operator-Smoothstep.md)
        * [Square Root](Operator-SquareRoot.md)
        * [Step](Operator-Step.md)
        * [Subtract](Operator-Subtract.md)
      * Clamp
        * [Ceiling](Operator-Ceiling.md)
        * [Clamp](Operator-Clamp.md)
        * [Discretize](Operator-Discretize.md)
        * [Floor](Operator-Floor.md)
        * [Maximum](Operator-Maximum.md)
        * [Minimum](Operator-Minimum.md)
        * [Round](Operator-Round.md)
        * [Saturate](Operator-Saturate.md)
      * Constants
        * [Epsilon](Operator-Epsilon.md)
        * [Pi](Operator-Pi.md)
      * Coordinates
        * [Polar to Rectangular](Operator-PolarToRectangular.md)
        * [Rectangular to Polar](Operator-RectangularToPolar.md)
        * [Rectangular to Spherical](Operator-RectangularToSpherical.md)
        * [Spherical to Rectangular](Operator-SphericalToRectangular.md)
      * Exp
        * [Exp](Operator-Exp.md)
      * Geometry
        * [Area (Circle)](Operator-Area(Circle).md)
        * [Change Space](Operator-ChangeSpace.md)
        * [Distance (Line)](Operator-Distance(Line).md)
        * [Distance (Plane)](Operator-Distance(Plane).md)
        * [Distance (Sphere)](Operator-Distance(Sphere).md)
        * [InvertTRS (Matrix)](Operator-InvertTRS(Matrix).md)
        * [Transform (Direction)](Operator-Transform(Direction).md)
        * [Transform (Matrix)](Operator-Transform(Matrix).md)
        * [Transform (Position)](Operator-Transform(Position).md)
        * [Transform (Vector)](Operator-Transform(Vector).md)
        * [Transform (Vector4)](Operator-Transform(Vector4).md)
        * [Transpose (Matrix)](Operator-Transpose(Matrix).md))
        * [Volume (Axis Aligned Box)](Operator-Volume(AxisAlignedBox).md)
        * [Volume (Cone)](Operator-Volume(Cone).md)
        * [Volume (Cylinder)](Operator-Volume(Cylinder).md)
        * [Volume (Oriented Box)](Operator-Volume(OrientedBox).md)
        * [Volume (Sphere)](Operator-Volume(Sphere).md)
        * [Volume (Torus)](Operator-Volume(Torus).md)
      * Log
        * [Log](Operator-Log.md)
      * Remap
        * [Remap](Operator-Remap.md)
        * [Remap [0..1] => [-1..1]](Operator-Remap(-11).md)
        * [Remap [-1..1] => [0..1]](Operator-Remap(01).md)
      * Trigonometry
        * [Acos](Operator-Acos.md)
        * [Asin](Operator-Asin.md)
        * [Atan](Operator-Atan.md)
        * [Atan2](Operator-Atan2.md)
        * [Cosine](Operator-Cosine.md)
        * [Sine](Operator-Sine.md)
        * [Tangent](Operator-Tangent.md)
      * Vector
        * [Append Vector](Operator-AppendVector.md)
        * [Construct Matrix](Operator-ConstructMatrix.md)
        * [Cross Product](Operator-CrossProduct.md)
        * [Distance](Operator-Distance.md)
        * [Dot Product](Operator-DotProduct.md)
        * [Length](Operator-Length.md)
        * [Look At](Operator-LookAt.md)
        * [Normalize](Operator-Normalize.md)
        * [Rotate 2D](Operator-Rotate2D.md)
        * [Rotate 3D](Operator-Rotate3D.md)
        * [Sample Bezier](Operator-SampleBezier.md)
        * [Squared Distance](Operator-SquaredDistance.md)
        * [Squared Length](Operator-SquaredLength.md)
        * [Swizzle](Operator-Swizzle.md)
      * Wave
        * [Sawtooth Wave](Operator-SawtoothWave.md)
        * [Sine Wave](Operator-SineWave.md)
        * [Square Wave](Operator-SquareWave.md)
        * [Triangle Wave](Operator-TriangleWave.md)
    * Noise
      * [Cellular Curl Noise](Operator-CellularCurlNoise.md)
      * [Cellular Noise](Operator-CellularNoise.md)
      * [Perlin Curl Noise](Operator-PerlinCurlNoise.md)
      * [Perlin Noise](Operator-PerlinNoise.md)
      * [Value Curl Noise](Operator-ValueCurlNoise.md)
      * [Value Noise](Operator-ValueNoise.md)
    * Random
      * [Random Number](Operator-RandomNumber.md)
      * [Random Selector](Operator-RandomSelectorWeighted.md)
    * Sampling
      * [Buffer Count](Operator-BufferCount.md)
      * [Get Mesh Index Count](Operator-MeshIndexCount.md)
      * [Get Mesh Triangle Count](Operator-MeshTriangleCount.md)
      * [Get Mesh Vertex Count](Operator-MeshVertexCount.md)
      * [Get Skinned Mesh Index Count](Operator-SkinnedMeshIndexCount.md)
      * [Get Skinned Mesh Triangle Count](Operator-SkinnedMeshTriangleCount.md)
      * [Get Skinned Mesh Vertex Count](Operator-SkinnedMeshVertexCount.md)
      * [Get Skinned Mesh Local Root Transform](Operator-SkinnedLocalTransform.md)
      * [Get Skinned Mesh World Root Transform](Operator-SkinnedWorldTransform.md)
      * [Get Texture Dimensions](Operator-GetTextureDimensions.md)
      * [Load CameraBuffer](Operator-LoadCameraBuffer.md)
      * [Load Texture2D](Operator-LoadTexture2D.md)
      * [Load Texture2DArray](Operator-LoadTexture2DArray.md)
      * [Load Texture3D](Operator-LoadTexture3D.md)
      * [Position (Depth)](Operator-Position(Depth).md)
      * [Sample Buffer](Operator-SampleBuffer.md )
      * [Sample CameraBuffer](Operator-SampleCameraBuffer.md)
      * [Sample Curve](Operator-SampleCurve.md)
      * [Sample Gradient](Operator-SampleGradient.md)
      * [Sample Mesh](Operator-SampleMesh.md)
      * [Sample Mesh Index](Operator-SampleMeshIndex.md)
      * [Sample Skinned Mesh](Operator-SampleSkinnedMesh.md)
      * [Sample Skinned Mesh Renderer Index](Operator-SampleMeshIndex.md)
      * [Sample Signed Distance Field](Operator-SampleSDF.md)
      * [Sample Texture2D](Operator-SampleTexture2D.md)
      * [Sample Texture2DArray](Operator-SampleTexture2DArray.md)
      * [Sample Texture3D](Operator-SampleTexture3D.md)
      * [Sample TextureCube](Operator-SampleTextureCube.md)
      * [Sample TextureCubeArray](Operator-SampleTextureCubeArray.md)
      * [Sample Attribute Map](Operator-SampleAttributeMap.md)
    * Spawn
      * [Spawn State](Operator-SpawnState.md)
    * Utility
      * [Point Cache](Operator-PointCache.md)
* Performance and Optimization
  * [Profiling and Debug Panels](performance-debug-panel.md)
* Reference
  * [Standard Attributes](Reference-Attributes.md)
  * [Types](VisualEffectGraphTypeReference.md)
    * [AABox](Type-AABox.md)
    * [ArcCircle](Type-ArcCircle.md)
    * [ArcCone](Type-ArcCone.md)
    * [ArcSphere](Type-ArcSphere.md)
    * [ArcTorus](Type-ArcTorus.md)
    * [Camera](Type-Camera.md)
    * [CameraBuffer](Type-CameraBuffer.md)
    * [Circle](Type-Circle.md)
    * [Cone](Type-Cone.md)
    * [Cylinder](Type-Cylinder.md)
    * [Direction](Type-Direction)
    * [Line](Type-Line.md)
    * [OrientedBox](Type-OrientedBox.md)
    * [Plane](Type-Plane.md)
    * [Position](Type-Position)
    * [Sphere](Type-Sphere.md)
    * [TerrainType](Type-TerrainType.md)
    * [Torus](Type-Torus.md)
    * [Transform](Type-Transform.md)
    * [Vector](Type-Vector.md)
