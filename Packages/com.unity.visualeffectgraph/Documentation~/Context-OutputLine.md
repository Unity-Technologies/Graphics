# Output Particle Line

Menu Path : **Context > Output Particle Line**

This **Output Particle Line** Context uses lines to render a particle system. Lines are defined by two end-points and are always a single pixel width regardless of the distance of the particle to the camera or the particle's size and scale attributes.

Two modes are available to set the end point of the line. The first point is always at the particle position:

*   Using target offset in particle space. Specifies the second point via an offset defined in the particle space.
*   Using target position attribute. Specifies the second point with the target position attribute.

This output does not support texturing.

Below is a list of settings and properties specific to the Output Particle Line Context. For information about the generic output settings this Context shares with all other Contexts, see [Global Output Settings and Properties](Context-OutputSharedSettings.md).


## Context settings

| Setting | Type | Description |
| ------- | ---- | ----------- |
|**Use Target Offset**|bool|Indicates whether this Context derives the end point of the line as an offset to the particle position, in particle space. If you disable this property, the Context uses the target position attribute of the particle as the end point.|

## Context properties

| Input | Type | Description |
| ----- | ---- | ----------- |
|**Target Offset**|Vector3|The offset in particle space this Context applies to the particle position to derive the second point.<br/>This property is only available when you enable Use Target Offset.|
