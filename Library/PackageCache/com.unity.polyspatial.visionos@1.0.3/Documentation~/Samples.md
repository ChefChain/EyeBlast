---
uid: psl-vos-samples
---
# About _PolySpatial Samples_ 
Unity’s **PolySpatial Samples** provides a starting point for visionOS development in Unity centered around specific use cases with both bounded volume and unbounded experiences.


## Bounded Volume Samples

### Balloon Gallery

The **Balloon Gallery** is a mini-game that demonstrates targeted input using **Indirect Pinch and Direct (Poke)** input to target content in a bounded volume scene.

![balloon-gallery-1](images/samples/balloon-gallery-1.png "Balloon Gallery") ![balloon-gallery-2](images/samples/balloon-gallery-2.png "Balloon Gallery")


### Character Walker

The **Character Walker** is a mini-game that demonstrates the ability to dynamically reposition the volume camera in a bounded volume. The Character Walker mini-game follows the character as it navigates an environment that is larger than the extent of the bounded volume.

![character-runner-1](images/samples/character-runner-1.png "Character Runner") ![character-runner-2](images/samples/character-runner-2.png "Character Runner")


### Input Data Visualization

The **Input Data Visualization** scene allows users to test various input types; **Direct (Poke), Direct Pinch, Indirect Pinch,** as well as analyze that data using the **Debug UI**.

![debug-ui-1](images/samples/debug-ui-1.png "Debug UI") ![debug-ui-2](images/samples/debug-ui-2.png "Debug UI")


### Manipulation

The **Manipulation** scene allows users to manipulate various objects with different colliders shapes within a bounded volume.

![manipulation-1](images/samples/manipulation-1.png "Manipulation") ![manipulation-2](images/samples/manipulation-2.png "Manipulation")


### Spatial UI

The **Spatial UI** scene gives users an example of a common spatial UI controls used in a bounded application. This includes elements such as a button, slider, toggle, and dropdown.

![ui-1](images/samples/ui-1.png "UI") ![ui-2](images/samples/ui-2.png "UI")


### Project Launcher

The **Project Launcher** scene allows users to launch various Unity scenes from a bounded volume using a carousel style spatial UI.

![project-launcher-1](images/samples/project-launcher-1.png "Project Launcher") ![project-launcher-2](images/samples/project-launcher-2.png "Project Launcher")


## Unbounded Samples

### Image Tracking

The **Image Tracking** scene allows users to spawn content using predefined, unique image markers in an unbounded application.  This sample uses ARKit features which are not supported in the VisionOS simulator, you will need to run it on device.


### Mixed Reality

The **Mixed Reality** scene allows users to spawn content using a custom ARKit hand gesture in an unbounded application. It also visualizes plane data information in the physical environment.  This sample uses ARKit features which are not supported in the VisionOS simulator, you will need to run it on device.
