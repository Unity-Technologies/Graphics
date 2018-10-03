## Description

A [Master Node](Master-Node.md) for physically based rendering. Can be used in either **Metallic** or **Specular** workflow modes as defined by the **Workflow** dropdown parameter.

Values supplied to the input **Normal** are expected to be in tangent space. Use the [Transform Node](Transform-Node.md) to convert to tangent space.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Albedo      | Input | Vector 3 | None | Defines material's albedo value. Expected range 0 - 1. |
| Normal      | Input | Vector 3 | Tangent Space Normal | Defines material's normal value. Expects normals in tangent space.  |
| Emission      | Input | Vector 3 | None | Defines material's emission color value. Expects positive values.  |
| Metallic      | Input | Vector 1 | None | Defines material's metallic value where 0 is non-metallic and 1 is metallic. Only available in Metallic **Workflow** mode.  |
| Specular      | Input | Vector 3 | None | Defines material's specular color value. Expected range 0 - 1. Only available in Specular **Workflow** mode.  |
| Smoothness      | Input | Vector 1 | None | Defines material's smoothness value. Expected range 0 - 1.  |
| Occlusion      | Input | Vector 1 | None | Defines material's ambient occlusion value. Expected range 0 - 1.  |
| Alpha      | Input | Vector 1 | None | Defines material's alpha value. Used for transparency and/or alpha clip. Expected range 0 - 1.  |
| Alpha Clip Threshold      | Input | Vector 1 | None | Fragments with an alpha below this value will be discarded. Requires a node connection. Expected range 0 - 1. |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Workflow      | Dropdown | Metallic, Specular | Defines workflow mode for the material |
| Surface      | Dropdown | Opaque, Transparent | Defines if the material is transparent |
| Blend      | Dropdown | Alpha, Premultiply, Additive, Multiply | Defines blend mode of a transparent material |
| Two Sided      | Toggle | True, False | If true both front and back faces of the mesh are rendered |