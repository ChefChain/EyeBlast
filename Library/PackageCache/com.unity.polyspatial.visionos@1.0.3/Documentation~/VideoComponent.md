---
uid: psl-vos-video-component
---
# Video Player components
You can use either of two components to display video content on visionOS with PolySpatial:

- [VisionOSVideoComponent](#visionosvideocomponent)
- [VideoPlayer](#video-player)

Although either component can play video on visionOS, each component has advantages and disadvantages to consider.

## VisionOSVideoComponent 

In order to support video content on visionOS, PolySpatial currently includes a custom `VisionOSVideoComponent`. To use it, add the component to a GameObject and set its properties. The `Target Material Renderer` property identifies the `MeshRenderer` that will display the video content. Any MeshRenderer can be used. The `Clip` property identifies the `Video Clip` asset that you want to play, such as an `.mp4`.

A limitation of the current system is that the clip must be manually copied into a `../StreamingAssets/VisionOSVideoClips/` folder to display and render properly on visionOS. This folder must be relative from the project folder - for example, the full folder path may be `Assets/StreamingAssets/VisionOSVideoClips/`. Create this folder if it does not exist. Ensure that the clip is not just moved into this folder, but copied into it, so that there are two instances of it - one referenced by the `VisionOSVideoComponent` and one under the `StreamingAssets` folder. Refer to [Special folder names](xref:SpecialFolders) and [Streaming Assets](xref:StreamingAssets) for more information about the Unity `StreamingAssets` folder.

Refer to Unity documentation on the `StreamingAssets` folder for more information.

![VisionOSVideoComponent](images/ReferenceGuide/VisionOSVideoComponent.png)

The `VisionOSVideoComponent` component exposes the following properties:

| **Property** | **Description** |
| --- | --- |
| **TargetMaterialRenderer** | Reference to the MeshRenderer on which the video should render. The video will overwrite the current material on that MeshRenderer. |
| **Clip** | The video asset to be played. |
| **IsLooping** | Whether the video should repeat when playback reaches the end of the clip. |
| **PlayOnAwake** | Whether the video should start playing when `Awake()` is called.|
| **Mute** | When true, audio playback is suppressed; when false, the volume value is respected. |
| **Volume** | The current volume of audio playback for the clip, ranging between 0 and 1. |

At runtime, you can use the following methods to control video playback:

| **Method**                   | **Description**                                                                                                 |
|------------------------------|-----------------------------------------------------------------------------------------------------------------|
| **GetState()**               | Get current state of the video player.                                                                          |
| **Play()**                   | Starts playback.                                                                                                |
| **Stop()**                   | Stops the playback and sets the current playback time to 0.                                                     |
| **Pause()**                  | Pauses the playback and leaves the current playback time intact.                                                |
| **GetDirectAudioMute()**     | Get the direct-output audio mute status for the specified track - note that currently only track index 0 works. |
| **SetDirectAudioMute()**     | Set the direct-output audio mute status for the specified track - note that currently only track index 0 works. |
| **GetDirectAudioVolume()**   | Get the direct-output audio volume for the specified track - note that currently only track index 0 works.      |
| **SetDirectAudioVolume()**   | Set the direct-output audio volume for the specified track - note that currently only track index 0 works.      |

<a id="video-player"></a>
## Unity Video Player support

You can use Unity's default `VideoPlayer` component to play video on visionOS with Unity Editor version `2022.3.18f1` and above. To use this component with PolySpatial you must:
* Set the `VideoPlayer` `Render Mode` to `Render Texture`, and then supply the video player with a [RenderTexture](xref:psl-vos-render-textures) as a target texture.
* Apply this render texture to a material, and apply the material to a renderer as normal.
* Add a script that will call `PolySpatialObjectUtils.MarkDirty()` on the render texture attached to the VideoPlayer. This `PolySpatialObjectUtils.MarkDirty()` call should be called once per frame. Refer to the [RenderTexture](xref:psl-vos-render-textures) documentation for more information on manually dirtying RenderTextures.

Setting the `Render Mode` property on the `VideoPlayer` component to anything but `Render Texture` will result in a warning and the `VideoPlayer` will not render anything.


## Video Player vs VisionOS Video Component

There are tradeoffs that come with choosing one video component over the other. 

The `VisionOSVideoComponent` utilizes visionOS's native video player to display and render video, allowing it display video in a performant manner. However, the public API for this component is much more limited than that of the normal VideoPlayer component, and more complex video player functionality (such as Events and Delegates) cannot be used with it. Additionally, because the video texture is created in visionOS, there is no access to that video texture through Unity. Additionally, the `VisionOSVideoComponent` will not work with debugging tools like PlayToDevice. 

Using the normal `VideoPlayer` component comes with performance costs, because it must update the render texture each frame, but has the advantage of being able to utilize all of the Unity Video Player's extensive methods, events, and delegates. Additionally, you have access to the video texture and can use it like any other texture - for example, using it in a shader graph material. Using the normal `VideoPlayer` component also allows streaming over a network, and works with [Play To Device](xref:psl-play-to-device). 
