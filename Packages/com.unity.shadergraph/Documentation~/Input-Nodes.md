# Input Nodes

## Basic

|[Boolean](Boolean-Node.md)|[Color](Color-Node.md)|
|:--------:|:------:|
|![A flow diagram with a labeled node titled "Boolean" and an output labeled "out(B)". The output is connected via an arrow pointing outward, indicating the result of a Boolean operation.](images/BooleanNodeThumb.png)|![A diagram featuring a single element labeled **"Boolean"** with an output connection marked **"out(B)"**. An arrow extends from this output, indicating the flow of a Boolean value from the node. The design implies a logic or data flow representation centered around Boolean values.
](images/ColorNodeThumb.png)|
| Defines a constant Boolean value in the shader. | Defines a constant Vector 4 value in the shader using a Color field. |
|[**Constant**](Constant-Node.md)|[**Integer**](Integer-Node.md)|
|![A diagram with a node labeled "Constant" and an output labeled "Out(1)". Below it, the symbol "PI" is displayed, with a downward arrow pointing toward it. ](images/ConstantNodeThumb.png)|![A node labeled "Integer" with an output labeled "out(1)". Next to it is the number 0, indicating that the node outputs the integer value zero.](images/IntegerNodeThumb.png)|
|Defines a Float of a mathematical constant value in the shader.|Defines a constant Float value in the shader using an Integer field.|
|[**Slider**](Slider-Node.md)|[**Time**](Time-Node.md)|
|![A slider control with an output labeled "out(1)". Below the slider, there is a range indicated by Min 0 on the left and Max 1 on the right. The current slider value is shown as 0, suggesting it is set at the minimum end of the range.](images/SliderNodeThumb.png)|![A node labeled Time with multiple outputs, each named and connected as follows: Time(1), Sine Time(1), Cosine Time(1), Delta Time(1), Smooth Delta(1).](images/TimeNodeThumb.png)|
|Defines a constant Float value in the shader using a Slider field.|Provides access to various Time parameters in the shader.|
|[**Float**](Float.md)|[**Vector 2**](Vector-2-Node.md)|
|![A Vector node with a label for the X component set to 0. There is a connection marked X(1) leading to an output labeled out(1). An arrow extends from the output, indicating the flow of this vector’s X value through the system.](images/Vector1NodeThumb.png)|![a Vector 2 node with two components: X set to 0, connected via X(1) to an output labeled out(2), Y set to 0, connected via Y(1). Arrows indicate the flow of these two values from their respective outputs.](images/Vector2NodeThumb.png)|
|Defines a Float value in the shader.|Defines a Vector 2 value in the shader.|
|[**Vector 3**](Vector-3-Node.md)|[**Vector 4**](Vector-4-Node.md)|
|![A Vector 3 node with three components: X set to 0, connected via X(1) to an output labeled out(3), Y set to 0, connected via Y(1), Z set to 0, connected via Z(1); Each component has arrows indicating the flow of these values, representing a three-dimensional vector output.](images/Vector3NodeThumb.png)|![A Vector 4 node with four components: X set to 0, connected via X(1) to an output labeled Out(4), Y set to 0, connected via Y(1), Z set to 0, connected via Z(1), W set to 0, connected via W(1). Each component is linked with arrows, showing the flow of these four values representing a four-dimensional vector output.](images/Vector4NodeThumb.png)|
|Defines a Vector 3 value in the shader.|Defines a Vector 4 value in the shader.|

## Geometry

|[Bitangent Vector](Bitangent-Vector-Node.md)|[Normal Vector](Normal-Vector-Node.md)|
|:--------:|:------:|
|[![A Bitangent Vector node with an output labeled out(3). Below the label, there are two options or modes indicated: Space and World, with an arrow pointing down toward World. This suggests the vector can be interpreted or output in different coordinate spaces, with the current selection being World space.](images/BitangentVectorNodeThumb.png)](Combine-Node)|![A Normal Vector node with an output labeled Out(3). Below it, there are two options: Space and World, with an arrow pointing downward toward World. This indicates the vector can be output in different coordinate spaces, currently set to World space.](images/NormalVectorNodeThumb.png)|
| Provides access to the mesh vertex or fragment's Bitangent Vector. | Provides access to the mesh vertex or fragment's Normal Vector. |
|[**Position**](Position-Node.md)|[**Screen Position**](Screen-Position-Node.md)|
|![A Position node with an output labeled Out(3). Below it, there are two options: Space and World, with a downward arrow pointing at World. This indicates the position value can be represented in different coordinate spaces, currently set to World space.](images/PositionNodeThumb.png)|![A Position node with an output labeled Out(3). Beneath it, there are two selectable options: Space and World, with an arrow indicating the selection is set to World space. This suggests the position value is provided relative to the world coordinate system.](images/ScreenPositionNodeThumb.png)|
|Provides access to the mesh vertex or fragment's Position.|Provides access to the mesh vertex or fragment's Screen Position.|
|[**Tangent Vector**](Tangent-Vector-Node.md)|[**UV**](UV-Node.md)|
|![A Tangent Vector node with an output labeled Out(3). Below it, two options are listed: Space and World, with an arrow pointing downward toward World. This indicates the vector can be output in different coordinate spaces, currently set to World space.](images/TangentVectorNodeThumb.png)|![A Tangent Vector node with an output labeled Out(3). Below it, two options are listed: Space and World, with an arrow pointing downward toward World. This indicates the vector can be output in different coordinate spaces, currently set to World space.
](images/UVNodeThumb.png)|
|Provides access to the mesh vertex or fragment's Tangent Vector.|Provides access to the mesh vertex or fragment's UV coordinates.|
|[**Vertex Color**](Vertex-Color-Node.md)|[**View Direction**](View-Direction-Node.md)|
|![a Vertex Color node with an output labeled Out(4). An arrow extends from the output, indicating the flow of a four-component color value, typically representing RGBA data.](images/VertexColorNodeThumb.png)|![A View Direction node with an output labeled Out(3). Below it, two options are shown: Space and World, with a downward arrow pointing to World, indicating the current coordinate space for the view direction vector.](images/ViewDirectionNodeThumb.png)|
|Provides access to the mesh vertex or fragment's Vertex Color value.|Provides access to the mesh vertex or fragment's View Direction vector.|
|[**Vertex ID**](Vertex-ID-Node.md)|
|![A Vertex ID node with an output labeled out(1).](images/VertexIDNodeThumb.png)|
|Provides access to the mesh vertex or fragment's Vertex ID value.|


## Gradient

|[Blackbody](Blackbody-Node.md)|[Gradient](Gradient-Node.md)|
|:--------:|:------:|
|![A node labeled Tempnat with an output labeled out(3). An arrow points outward from the output.](images/BlackbodyNodeThumb.png)|![a Gradient node with an output labeled out(G). An arrow extends from the output.](images/GradientNodeThumb.png)|
| Samples a radiation based gradient from temperature input (in Kelvin).  | Defines a constant Gradient in the shader. |
|[Sample Gradient](Sample-Gradient-Node.md)|
|![A Sample Gradient node with two main elements: An input labeled Gradient, an output labeled out(4). There is also an input labeled X set to 0, connected via Time(1).](images/SampleGradientNodeThumb.png)|
| Samples a Gradient given the input of Time. |

## Matrix

|[Matrix 2x2](Matrix-2x2-Node.md)|[Matrix 3x3](Matrix-3x3-Node.md)|
|:--------:|:------:|
|![A Matrix 2x2 node with an output labeled Out(2x2). The matrix elements are all set to 0, arranged in a 2-by-2 grid.](images/Matrix2x2NodeThumb.png)|![A Matrix 3x3 node with an output labeled Out(3x3). The matrix is displayed as a 3-by-3 grid with all elements set to 0.](images/Matrix3x3NodeThumb.png)|
| Defines a constant Matrix 2x2 value in the shader. | Defines a constant Matrix 3x3 value in the shader. |
|[**Matrix 4x4**](Matrix-4x4-Node.md)|[**Transformation Matrix**](Transformation-Matrix-Node.md)|
|![A Matrix 4x4 node with an output labeled Out(4x4). The matrix is displayed as a 4-by-4 grid with all elements set to 0.](images/Matrix4x4NodeThumb.png)|![ A Transformation Matrix node with an output labeled Out(4x4). Below it, there is a dropdown or selection labeled Model with a downward arrow.](images/TransformationMatrixNodeThumb.png)|
|Defines a constant Matrix 4x4 value in the shader.|Defines a constant Matrix 4x4 value for a default Unity Transformation Matrix in the shader.|

## Mesh Deformation
| [Compute Deformation Node](Compute-Deformation-Node)         | [Linear Blend Skinning Node](Linear-Blend-Skinning-Node)     |
| :----------------------------------------------------------- | :----------------------------------------------------------- |
| ![A Compute Deformation node with three outputs: Deformed Position, Deformed Normal, Deformed Tangent.](images/ComputeDeformationNodeThumb.png)             | ![A Linear Blend Skinning node with three pairs of inputs and outputs: Vertex Position(3) input connected to Skinned Position(3) output, Vertex Normal(3) input connected to Skinned Normal(3) output, Vertex Tangent(3) input connected to Skinned Tangent(3) output.](images/LinearBlendSkinningNodeThumb.png)            |
| Passes compute deformed vertex data to a vertex shader. Only works with the [Entities Graphics package](https://docs.unity3d.com/Packages/com.unity.entities.graphics@latest/). | Applies Linear Blend Vertex Skinning. Only works with the [Entities Graphics package](https://docs.unity3d.com/Packages/com.unity.entities.graphics@latest/). |

## Sprite Deformation
| [Sprite Skinning Node](Sprite-Skinning-Node)     |
| :----------------------------------------------------------- |
| ![A Linear Blend Skinning node with three pairs of inputs and outputs, each handling 3-component vectors: Vertex Position(3) connected to Skinned Position(3) output, Vertex Normal(3) connected to Skinned Normal(3) output, Vertex Tangent(3) connected to Skinned Tangent(3) output.](images/SpriteSkinningNodeThumb.png)                 |
| Applies Vertex Skinning on Sprites. Only works with the [2D Animation](https://docs.unity3d.com/Packages/com.unity.2d.animation@latest/). |

## PBR

|    [**Dielectric Specular**](Dielectric-Specular-Node.md)    |      [**Metal Reflectance**](Metal-Reflectance-Node.md)      |
| :----------------------------------------------------------: | :----------------------------------------------------------: |
|       ![A Dielectric Specular node with an output labeled Out(1). Below it, there are settings including: Material with an option labeled Common and a dropdown arrow, Range set to LJ 0.5, IOR (Index of Refraction) set to 1.](images/DielectricSpecularNodeThumb.png)       |          ![A Metal Reflectance node with an output labeled out(3). Below it, there is a Material option set to Iron with a dropdown arrow.](images/MetalReflectanceNodeThumb.png)           |
| Returns a Dielectric Specular F0 value for a physically based material. | Returns a Metal Reflectance value for a physically based material. |


## Scene

|[Ambient](Ambient-Node.md)|[Camera](Camera-Node.md)|
|:--------:|:------:|
|![An Ambient node with three outputs: Color/Sky(3), Equator(3), Ground(3).](images/AmbientNodeThumb.png)|![A Camera node with multiple outputs: Position(3), Direction(3), Orthographic(1), Near Plane(1), Far Plane(1), Z Buffer Sign(1), Width(1), Height(1).](images/CameraNodeThumb.png)|
| Provides access to the Scene's Ambient color values. | Provides access to various parameters of the current Camera. |
|[**Fog**](Fog-Node.md)|[**Baked GI**](Baked-GI-Node.md)|
|![A Fog node with: An input labeled Object Space Position(3), outputs labeled Color(4) and Density(1).](images/FogNodeThumb.png)||
|Provides access to the Scene's Fog parameters.|Provides access to the Baked GI values at the vertex or fragment's position.|
|[**Object**](Object-Node.md)|[**Reflection Probe**](Reflection-Probe-Node.md)|
|![An Object node with two outputs: Position(3), Scale(3).](images/ObjectNodeThumb.png)|![A Reflection Probe node with several elements: An input labeled Object Space View Dir(3) connected to an output labeled Out(3), an input labeled Object Space Normal(3), an input labeled X set to 0, connected via LOD(1)](images/ReflectionProbeNodeThumb.png).|
|Provides access to various parameters of the Object.|Provides access to the nearest Reflection Probe to the object.|
|[**Scene Color**](Scene-Color-Node.md)|[**Scene Depth**](Scene-Depth-Node.md)|
|![A Scene Color node with: An input labeled Default, an output labeled Out(4).](images/SceneColorNodeThumb.png)|![A Scene Depth node with: An input labeled Default UV(4), an output labeled out(1), a setting or option labeled Sampling Linear 01.](images/SceneDepthNodeThumb.png)|
|Provides access to the current Camera's color buffer.|Provides access to the current Camera's depth buffer.|
|[**Screen**](Screen-Node.md)|[**Eye Index**](Eye-Index-Node.md)|
|![A Screen node with two outputs: Width(1), Height(1).](images/ScreenNodeThumb.png)|![An Eye Index node with an output labeled Out(1).](images/EyeIndexNodeThumb.png)|
|Provides access to parameters of the screen.|Provides access to the Eye Index when stereo rendering.|

## Texture

|[**Cubemap Asset**](Cubemap-Asset-Node.md)|[**Sample Cubemap**](Sample-Cubemap-Node.md)|
|:--------:|:------:|
|[![A Cubemap Asset node with an output labeled out(C). Below it, there is a dropdown or selector displaying None (Cubemap).](images/CubemapAssetNodeThumb.png)](Combine-Node)|![A Sample Cubemap node with: An input labeled None (Cubemap), an input labeled World Space connected to pijr(3), an input labeled x set to 0, connected to LoD(1), an output labeled Out(4).](images/SampleCubemapNodeThumb.png)|
| Defines a constant Cubemap Asset for use in the shader. | Samples a Cubemap and returns a Vector 4 color value for use in the shader. |
|[**Sample Reflected Cubemap Node**](Sample-Reflected-Cubemap-Node.md)|[**Sample Texture 2D**](Sample-Texture-2D-Node.md)|
|![A Sample Cubemap node with several inputs and an output: An input labeled None (Cubemap) connected to Cube(C), inputs labeled Object Space ViewDir(3) and Object Space Normal(3), an input labeled Sampler(SS), an input labeled X set to 0, connected to LOD(1), an output labeled Out(4).](images/SampleReflectedCubemapThumb.png)|![A Sample Texture 2D node with: An input labeled None (Texture) connected to Texture(T2), an input labeled UV(2), an input labeled Sampler(SS), multiple outputs labeled RGBA(4), R(1), G(1), B(1), and A(1), two dropdown options: Type set to Default, and Space set to Tangent.](images/SampleTexture2DNodeThumb.png)|
|Samples a Cubemap with reflected vector and returns a Vector 4 color value for use in the shader.|Samples a Texture 2D and returns a color value for use in the shader.|
|[**Sample Texture 2D Array**](Sample-Texture-2D-Array-Node.md)|[**Sample Texture 2D LOD**](Sample-Texture-2D-LOD-Node.md)|
|![A Sample Texture 2D Array node with: An input labeled None (Texture) connected to Texture Array (T2A), an input labeled X set to 0, connected to Index(1), an input labeled UV connected to UV(2), an input labeled Sampler(SS), multiple outputs labeled RGBA(4), R(1), G(1), B(1), and A(1).](images/SampleTexture2DArrayNodeThumb.png)|![A Sample Texture 2D LOD node with: An input labeled None (Texture) connected to Texture(T2), an input labeled UV(2), an input labeled Sampler(SS), an input labeled X set to 0, connected to LOD(1), multiple outputs labeled RGBA(4), R(1), G(1), B(1), and A(1), two dropdown options: Type set to Default, and Space set to Tangent.](images/SampleTexture2DLODNodeThumb.png)|
|Samples a Texture 2D Array at an Index and returns a color value for use in the shader.|Samples a Texture 2D at a specific LOD and returns a color value for use in the shader.|
|[**Sample Texture 3D**](Sample-Texture-3D-Node.md)| [**Sample Virtual Texture**](Sample-Virtual-Texture-Node.md) |
|![A Sample Texture 3D node with: An input labeled None (Texture) connected to Texture(T3), inputs labeled X 0, Y 0, Z 0 combined as UV(3), an input labeled Sampler(SS), an output labeled Out(4).](images/SampleTexture3DNodeThumb.png)| ![A Sample Virtual Texture node with: An input labeled UV(2), an input labeled VT(VT) connected to Virtual Texture (VT), four outputs labeled Out(4), Out2(4), Out3(4), and Out4(4).](images/SampleVirtualTextureNodeThumb.png) |
|Samples a Texture 3D and returns a color value for use in the shader.| Samples a Virtual Texture and returns color values for use in the shader.|
|[**Sampler State**](Sampler-State-Node.md)|[**Texture Size**](Texture-Size-Node.md)|
|![A Sampler State node with an output labeled Out(SS). Below it are settings including: Filter set to Linear, Wrap set to Repeat.](images/SamplerStateNodeThumb.png)|![A Texture Size node with: An input labeled None (Texture) connected to Texture(T2), outputs labeled Width(1), Height(1), Texel Width(1), and Texel Height(1).](images/TexelSizeNodeThumb.png) <!-- Add updated image -->|
|Defines a Sampler State for sampling textures.|Returns the Width and Height of the texel size of Texture 2D input.|
|[**Texture 2D Array Asset**](Texture-2D-Array-Asset-Node.md)|[**Texture 2D Asset**](Texture-2D-Asset-Node.md)|
|![A Texture 2D Array Asset node with an output labeled Out(T2A). Below it, there is a dropdown or selector displaying None (Texture 2D Array).](images/Texture2DArrayAssetNodeThumb.png)|![A Texture 2D Asset node with an output labeled Out(T2). Below it, there is a dropdown or selector displaying None (Texture).](images/Texture2DAssetNodeThumb.png)|
|Defines a constant Texture 2D Array Asset for use in the shader.|Defines a constant Texture 2D Asset for use in the shader.|
|[**Texture 3D Asset**](Texture-3D-Asset-Node.md)| |
|![A Texture 3D Asset node with an output labeled Out(T3). Below it, there is a dropdown or selector displaying None (Texture 3D).](images/Texture3DAssetNodeThumb.png)| |
|Defines a constant Texture 3D Asset for use in the shader.| |
