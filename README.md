# ComputeShaderBVHMeshHit
Unity ComputeShader implementation of [BVH(Bounding Volume Hierarchy)](https://en.wikipedia.org/wiki/Bounding_volume_hierarchy) based mesh hit checking.

![meshhit](/Documentations/meshhit.gif)
![bvh](/Documentations/bvh.gif)

# Installation

Add the following address to UnitPackageManager.  
`https://github.com/fuqunaga/ComputeShaderBVHMeshHit.git?path=/Packages/ComputeShaderBVHMeshHit`


# How to use
### Create BVH Asset
![BuilderWindow](/Documentations/BuilderWindow.png)
1. **Window > BvhBuilder**
1. Set `meshObjectRoot` object.
2. **Build** to create BvhAsset.

### C#
1. Put BvhHelperBehaviour to the Hierarchy.
1. Set BvhAsset.
1. Call `BvhHelperBehaviour.SetBuffersToComputeShader()`.

### ComputeShader
1. Add the following include statement to your ComputeShader.  
`#include "Packages/ga.fuquna.computeshaderbvhmeshhit/Bvh.hlsl"`
1. Call `TraverseBvh()` to detect a mesh hit.


# References
* http://raytracey.blogspot.com/2016/01/gpu-path-tracing-tutorial-3-take-your.html
* http://marupeke296.com/COL_3D_No18_LineAndAABB.html
* https://shikousakugo.wordpress.com/2012/06/27/ray-intersection-2/
