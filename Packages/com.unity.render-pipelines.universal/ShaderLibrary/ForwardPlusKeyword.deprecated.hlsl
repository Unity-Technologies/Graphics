#ifndef UNIVERSAL_FORWARD_PLUS_KEYWORD_DEPRECATED_INCLUDED
#define UNIVERSAL_FORWARD_PLUS_KEYWORD_DEPRECATED_INCLUDED

// _FORWARD_PLUS keyword deprecated in 6.1
// We will emit a warning and define deprecated macros for backwards compatibility.
// This file will be removed in a future release.

// To upgrade custom shaders, replace all instances of the deprecated macros (left) with the new macros (right):
// _FORWARD_PLUS                         _CLUSTER_LIGHT_LOOP
// USE_FORWARD_PLUS                      USE_CLUSTER_LIGHT_LOOP
// FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK  CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
#if defined(_FORWARD_PLUS_KEYWORD_DECLARED) || defined(_FORWARD_PLUS)
#warning _FORWARD_PLUS shader keyword has been deprecated. Please update your shaders to use _CLUSTER_LIGHT_LOOP shader keyword instead, otherwise shader compilation times may be negatively affected.
#endif

#if defined(_FORWARD_PLUS)
#define USE_FORWARD_PLUS USE_CLUSTER_LIGHT_LOOP
#define FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
#if !defined(_CLUSTER_LIGHT_LOOP)
#define _CLUSTER_LIGHT_LOOP 1
#endif
#endif

#endif
