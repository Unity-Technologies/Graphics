# Shader Graph Preferences

This window is where the Shader Graph project wide settings are listed.

## Settings
| Setting | Description |
|:------- |:----------- |
|Shader Variant Limit| It is possible to add keywords to a graph to create shader variants and is a good practice when creating "uber shaders". The number of shader variants a shader should use is largely determined by the target platform, and every keyword added at minimum doubles the number of variants for a graph. Instead of allowing a user's hardware to dictate the max number of shader variants, this setting can be set and any graph that exceeds this limit will throw an error in the graph. | 
