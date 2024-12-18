#ifndef UNIVERSAL_FORWARD_PLUS_KEYWORD_DEPRECATED_INCLUDED
#define UNIVERSAL_FORWARD_PLUS_KEYWORD_DEPRECATED_INCLUDED

// _FORWARD_PLUS keyword deprecated in 6.1
// We will emit a warning and define _CLUSTER_LIGHT_LOOP for backwards compatibility.
// This block will be removed in a future release.
#if defined(_FORWARD_PLUS_KEYWORD_DECLARED) || defined(_FORWARD_PLUS)
#warning _FORWARD_PLUS shader keyword has been deprecated. Please update your shaders to use _CLUSTER_LIGHT_LOOP shader keyword instead, otherwise shader compilation times may be negatively affected.
#endif

#if defined(_FORWARD_PLUS) && !defined(_CLUSTER_LIGHT_LOOP)
#define _CLUSTER_LIGHT_LOOP 1
#endif

#endif
