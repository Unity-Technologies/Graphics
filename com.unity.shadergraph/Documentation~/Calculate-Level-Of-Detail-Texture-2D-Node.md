Calculate Level Of Detail Texture 2D Node
Description
This node is designed to work with Texture2D. It has a clamped and unclamped mode. It maps to the CalculateLevelOfDetail and CalculateLevelOfDetailUnclamped HLSL intrinsic functions.
On hardware where those intrinsics don't exist, Shader Graph determines a fallback approximation.
