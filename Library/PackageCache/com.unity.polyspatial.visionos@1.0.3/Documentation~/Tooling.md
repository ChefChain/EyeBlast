---
uid: psl-vos-tooling
---
# PolySpatial Tooling

## Logging
PolySpatial logging messages are tagged by category and level to facilitate more targeted debugging. From the main menu of the Unity Editor, select **Window &gt; PolySpatial &gt; Logging** to open the PolySpatial Logging window. From here, you can toggle which categories are enabled, whether they should generate stack traces, and for categories for which stack traces are enabled, what levels will generate the traces. 

## Debug Links
To facilitate debugging in Play Mode, the PolySpatial runtime adds **DebugPolySpatialGameObjectLinks** components to connect each **simulation** GameObject to its corresponding **backing** GameObject in the Unity SceneGraph.

## Recording & Playback
<a name="recording-and-playback"></a>
Recording & Playback lets users record a PolySpatial PlayMode session to a file, and then play it back.
To open the Recording & Playback window, go to **Windows &gt; PolySpatial &gt; Recording and Playback**.

### Recording
Press the `Record` button to enter Play mode and start recording. Perform interactions and supply input normally, then exit play mode, or press the `Stop Recording` button to stop recording. A new file will be added to the recordings list.
Recordings are saved in `Library/PolySpatialRecordings` and should be playable on any machine using the same version of the PolySpatial package.

#### Recording Framerate
Enter a positive `Recording Framerate` value, and enable `Limit Framerate While Recording` to limit recording framerate to this value. Otherwise, recording framerate is set to unlimited.

### Playback
To replay a recording, select it from the recordings list, and press the `Play` button. New input won't be processed, but the input encoded in the original recording will replay.

### Usages
Among other things, theses files can be submitted to Unity support allowing us to debug many project-specific issues without needing a full zip of your project.
