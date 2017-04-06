// There is two kind of lighting architectures.
// Those that are control from inside the "Material".shader with "Pass" concept like forward lighting. Call later forward lighting architecture.
// Those that are control outside the "Material".shader in a "Lighting".shader like deferred lighting. Call later deferred lighting architecture.

// When dealing with deferred lighting architecture, the renderloop is in charge to call the correct .shader.
// RenderLoop can do multiple call of various deferred lighting architecture.
// (Note: enabled variant for deferred lighting architecture are in deferred.shader)
// When dealing with forward lighting architecture, the renderloop must specify a shader pass (like "forward") but it also need
// to specify which variant of the forward lighting architecture he want (with cmd.EnableShaderKeyword()).
// Renderloop can suppose dynamically switching from regular forward to tile forward for example within the same "Forward" pass.

// The purpose of the following pragma is to define the variant available for "Forward" Pass in "Material".shader.
// If only one keyword is present it mean that only one type of forward lighting architecture is supported.

// Must match name in GetKeyword() method of forward lighting architecture .cs file
// #pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS -> can't use a pragma from include... (for now)

// No USE_FPTL_LIGHTLIST as we are in forward and this use the cluster path (but cluster path can use the tile light list for opaque)
#define USE_CLUSTERED_LIGHTLIST
#define LIGHTLOOP_TILE_ALL
