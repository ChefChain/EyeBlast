---
uid: psl-vos-render-textures
---
# PolySpatial Render Texture Support
PolySpatial transfers RenderTextures to host platforms using an optimized path.  On visionOS, this is performed by using a GPU blit to copy the contents of the RenderTexture to a texture provided by RealityKit.  Transferring large numbers of RenderTextures at once and/or RenderTextures with large dimensions may still incur a performance penalty, however.

## Formats
Formats supported for RenderTextures on visionOS are limited by the underlying [DrawableQueue](https://developer.apple.com/documentation/realitykit/textureresource/drawablequeue-swift.class) API.  The only formats currently tested are `R8G8B8A8_UNORM` (the default RenderTexture format) and `R16G16B16A16_SFLOAT`.  Other formats may or may not work; we recommend experimenting to find out.

## Batch mode rendering
Mixed reality apps for visionOS run in batch mode, which means that cameras do not render automatically.  If you use a RenderTexture as the `Output Texture` of a camera, you may use the following script to ensure that the camera renders once per update.
```
using UnityEngine;
using Unity.PolySpatial;

public class BatchModeUpdateRenderer : MonoBehaviour
{
    Camera m_Camera;

    void Start()
    {
        m_Camera = GetComponent<Camera>();
    }

    void Update()
    {
        if (Application.isBatchMode && m_Camera)
            m_Camera.Render();
    }
}
```

## Manual dirtying
If you update a RenderTexture outside of a call to `Camera.Render` (for example, by setting [RenderTexture.active](https://docs.unity3d.com/ScriptReference/RenderTexture-active.html) and invoking immediate mode rendering commands), PolySpatial will not automatically transfer the RenderTexture's new contents.  Instead, you must manually dirty the RenderTexture using `Unity.PolySpatial.PolySpatialObjectUtils.MarkDirty(renderTexture)` each frame that it is updated.  This will cause PolySpatial to send the new contents of the RenderTexture to the host.

```
using UnityEngine;
using Unity.PolySpatial;

public class SetRenderTextureDirty : MonoBehaviour
{
    public RenderTexture texture;

    void Update()
    {
        Unity.PolySpatial.PolySpatialObjectUtils.MarkDirty(texture);
    }
}
```


