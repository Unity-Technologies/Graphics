# HD Sun Light Direction Node

Provides access to the direction of the main directional light used by HDRP.
The main directional light is the one casting shadows if there is any. Otherwise, it fallbacks to the first non shadow casting directional light.

### Available Ports

| Name          | Direction | Type           | Description                                               |
| :------------ | :-------- | :------------- | :-------------------------------------------------------- |
| **Output**    | Output    | Vector3        | The normalized direction of the sun light in world space. |
