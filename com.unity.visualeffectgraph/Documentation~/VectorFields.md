<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This feature is currently experimental and is subject to change in later major versions. To use this feature, enable <b>Experimental Operators/Blocks</b> in the <b>Visual Effects</b> tab of your Project's Preferences.</div>
# Vector Fields / Signed Distance Fields

Vector Fields and Signed Distance Fields are 3D Fields containing values stored in voxels. These are available as 3D Textures in Visual Effect Graph and can be imported using the Volume File (`.vf`) file format.

Volume File is an [Open Source specification](https://github.com/peeweek/VectorFieldFile/blob/master/README.md) that contains basic structure for storing floating point data. Unity automatically imports  Volume File files are automatically imported in unity as 3D Textures and can be used in Visual Effect Graph Blocks and Operators that input 3D Textures, such as Vector Field or Signed Distance Field Blocks.

## Vector Field Importer

![](Images/VectorFieldInspector.png)

Unity provides the following settings in the Inspector to import Volume File files:

* **Output Format :** Precision of the Output 3D Texture
  * Half : Floating Point with 16-bit Half Precision
  * Float : Floating Point with 32-bit Single Precision
  * Byte : Unsigned Fixed Point values of 8-bit Precision
* **Wrap Mode :** Wrap Mode of the output Texture
* **Filter Mode :** Filter Mode of the output Texture
* **Generate Mip Maps :** Whether to generate mip-maps for the texture
* **Aniso Level :** Anisotropy Level

## Generating Vector Field Files

You can generate vector fields in one of the following ways:

- Use the Houdini VF Exporter bundled with [VFXToolbox](https://github.com/Unity-Technologies/VFXToolbox) (located in the /DCC~ folder)
- Write your own exporter to write [VF Files](https://github.com/peeweek/VectorFieldFile/blob/master/README.md) files that follow the specification.
