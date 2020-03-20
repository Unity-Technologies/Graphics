<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This Feature is currently experimental and is subject to change in later major versions.</div>

# Mesh sampling

The mesh sampling support isn't feature complete, you are able to fetch vertices of a mesh but there isn't access to the triangle topology yet.

Visual Effect Graph provides two way to access this feature :

### Position (Mesh)

<img src="Images/PositionMesh.png" style="zoom:78%;" />

This block set the position reading vertex position attribute and direction reading normal attribute.

### Sample Mesh

<img src="Images/SampleMesh.png" alt="image-20200320154843722" style="zoom:67%;" />

This operator provides more custom read, you can choose outputted [VertexAttribute](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.html) in inspector

## Limitation

- Only support [VertexAttributeFormat.Float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html) for all  [VertexAttribute](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.html) expect [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html) which has to be a four component attribute using [VertexAttributeFormat.UInt8](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html) format
- TexCoord is limited and constrained to two dimensions attributes.
- The sampled mesh should be in [readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html), otherwise, operator and block will only return zero values.

