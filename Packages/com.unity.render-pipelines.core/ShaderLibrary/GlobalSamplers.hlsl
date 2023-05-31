#ifndef UNITY_CORE_SAMPLERS_INCLUDED
#define UNITY_CORE_SAMPLERS_INCLUDED

// Common inline samplers.
// Separated into its own file for robust including from any other file.
// Helps with sharing samplers between intermediate and/or procedural textures (D3D11 has a active sampler limit of 16).
SAMPLER(sampler_PointClamp);
SAMPLER(sampler_LinearClamp);
SAMPLER(sampler_PointRepeat);
SAMPLER(sampler_LinearRepeat);

#endif //UNITY_CORE_SAMPLERS_INCLUDED
