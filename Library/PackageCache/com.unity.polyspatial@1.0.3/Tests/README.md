# PolySpatial Tests

The purpose of this document is to cover some general guidelines for writing, executing and updating PolySpatial tests.
For any PolySpatial test related questions, feel free to reach out to `manuel.gonzalez@unity3d.com`
## Local Test Scripts
TBD

## Yamato Test Jobs
TBD

## Editor Tests
TBD

## Runtime Tests
### Helper Classes
There are several helper and utility classes available for use when writing new tests. See the following directories below for each available utilities modules and classes.
* `Tests/Runtime/PolySpatialTest/Scripts` - a collection of `MonoBehaviour` scripts that can be used to transform test GameObjects
* `Tests/Runtime/Utils/PolySpatialStateValidator.cs` - a utility class that retrieves, diffs and generates diff strings for PolySpatial GameObject state.
   This class fetches supported GameObject data and diffs between simulation and renderer layers.
* `Tests/Runtime/Utils/ScreenValidator.cs` - a utility class that captures and retrieves renderer content as images for image comparison and validation.

### Unit Tests
TBD

### Integration Tests

#### Frame Capture Tests

The frame capture tests compare runtime frames captured to reference images, or compare frames captured from Unity
rendering with the equivalent Unity objects from PolySpatial.

##### Creating Frame Capture Tests

1. Create a new scene under `Tests/Scenes/FrameCapture`, using a naming convetion of `Foo[[_II]_CC].unity`, where CC=the number of frames to capture (count),
   and II=the frame interval. For example, `Foo_3.unity` would capture 3 frames using the default 5 frame interval. `Foo_1_10.unity` would capture 10 frames
   with a 1 frame (every frame) interval.
2. Tests are either "Unity to Unity", in which there is no reference image, but an image is captured from the Unity scene and a separate one from the
   PolySpatial scene (looking at only PolySpatial-layer objects), or they can be Backend captures.
3. If tests are Backend captures, reference images live in `Assets/Tests/ReferenceImages~/<<backend name>>/TestName_XX.png` where XX is the frame number.
4. The `Tools > PolySpatial Image Test Results` window can be used to view and compare test results. Reference images can also be accepted and updated.

