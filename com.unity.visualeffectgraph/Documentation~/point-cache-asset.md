# Point Cache asset

The Point Cache asset follows the open-source [Point Cache](https://github.com/peeweek/pcache/blob/master/README.md) specification and uses the `.pCache` file extension. Internally, these assets are nested [Scriptable Objects](https://docs.unity3d.com/Manual/class-ScriptableObject.html) and contain all the various textures that represent the maps of particle attributes.



Point Cache assets are read-only, so if you select one and view it in the Inspector, you cannot edit any of its properties. However, the Inspector displays the values of each read-only property. For information about what each read-only property means, see [Properties](#properties).

## Properties

A Point Cache asset displays read-only information such as the number of particles it contains and the textures that represent the particle properties.

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Script**      | Specifies the importer Unity uses to import the `.pCache` file. |
| **Point Count** | The number of particles this Point Cache represents.         |
| **Surface**     | A list of textures that represent the attribute maps for the particles. The name of the texture in each index of the array is the same as the name of the attribute the map is for. |
