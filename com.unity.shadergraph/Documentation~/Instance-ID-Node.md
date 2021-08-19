# Instance ID Node

## Description

When Unity renders with GPU instancing, it assigns an **Instance ID** to each geometry.

Use this node to capture **Instance ID** values in [`Graphics.DrawMeshInstanced`](https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html) API calls.

When Unity does not render with GPU instancing, this ID is 0.

When Unity uses dynamic instancing, instance IDs might not be consistent across multiple frames.

## Ports

| Name   | Direction  | Type  | Binding | Description |
|:-------|:-----------|:------|:--------|:------------|
| Out    | Output     | Float | None    | **Instance ID** for mesh of a given instanced draw call. |
