## Description

Returns a reflection vector using input **In** and a surface normal **Normal**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Incident vector value |
| Normal      | Input      |   Dynamic Vector | Normal vector value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = reflect(In, Normal);`