# Get Main Light Direction Node

## Description

Provides access to the direction of the main directional light in the scene.
The main directional light is the one casting shadows if there is any. Otherwise, it fallbacks to the first non shadow casting directional light.

### Ports

| Name          | Direction | Type           | Description                                               |
| :------------ | :-------- | :------------- | :-------------------------------------------------------- |
| **Direction** | Output    | Vector3        | The normalized direction of the sun light in world space. |
