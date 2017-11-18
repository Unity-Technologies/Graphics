// There is two kind of lighting architectures.
// Those that are control from inside the "Material".shader with "Pass" concept like forward lighting. Call later forward lighting architecture.
// Those that are control outside the "Material".shader in a "Lighting".shader like deferred lighting. Call later deferred lighting architecture.

// When dealing with deferred lighting architecture, the renderPipeline is in charge to call the correct .shader.
// renderPipeline can do multiple call of various deferred lighting architecture.
// (Note: enabled variant for deferred lighting architecture are in deferred.shader)
// When dealing with forward lighting architecture, the renderPipeline must specify a shader pass (like "forward") but it also need
// to specify which variant of the forward lighting architecture he want (with cmd.EnableShaderKeyword()).

// The purpose of the following pragma is to define the variant available for "Forward" Pass in "Material".shader.
// If only one keyword is present it mean that only one type of forward lighting architecture is supported.

// Must match name in GetKeyword() method of forward lighting architecture .cs file
// #pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS -> can't use a pragma from include... (for now)

// Forward transparent surface use clustering, forward opaque use FPTL
#ifdef _SURFACE_TYPE_TRANSPARENT
#define USE_CLUSTERED_LIGHTLIST
#else
#define USE_FPTL_LIGHTLIST
#endif
