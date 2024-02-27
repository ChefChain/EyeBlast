---
uid: psl-vos-glossary
---
# Glossary

- **PolySpatial Core** (com.unity.polyspatial): The foundational PolySpatial Package, where initialization and all setup begins. It performs change tracking and processing, serialization/deserialization, and includes the Shader Graph to MaterialX converter

- **PolySpatial XR** (com.unity.polyspatial.xr): Includes scene validation, capability profiles, building blocks, and coaching UI. Adds package dependencies on XRI, AR Foundation, and XR hands.

- **Unity PolySpatial -- Apple visionOS support** (com.unity.polyspatial.visionos): Adds a new build target (visionOS) and platform support for visionOS and Apple Vision Pro.

- **PolySpatial App** (aka **Client App** or **Unity App**): A Unity app (Unity player) that uses PolySpatial. PolySpatial apps are split in two logical parts: the **Unity Sim** and the **Backend**. 

- **Unity Sim**: The non-rendering portion of a Unity app - its application-specific logic, as well as built-in simulation features including physics, animation, AI, and asset management.
    - **(Unity) Sim Space**: The world space of a Unity Sim. While a typical Unity app simulates and renders objects in the same space, these may differ in a PolySpatial app.
    - **(Unity) Sim Physics**: The physics and colliders of the Unity Sim.

- **Vanilla Unity**: In the context of PolySpatial, Vanilla Unity refers to a non-PolySpatial Unity app. 

- **PolySpatial Host** (or Backend): The system that's responsible for actually rendering the objects controlled by the **Unity Sim**.
    - **Host (or Backend) Space**: The world space of the backend or Host in which a PolySpatial app is running. This may differ from **Unity Sim Space** because the host environment may allow apps to be moved around independently (for example, relocated to another position and volume in the real world).
    - **Host (or Backend) Physics**: The physics and colliders implemented within the backend to model the full shared environment for purposes such as input, selection, and cross-app interactions.


- **PolySpatial Layer**: A Unity layer that is created to house the backing objects of the Unity SceneGraph. If no such layer already exists *and* there are no free layers in which to create a new layer, the PolySpatial runtime will not initialize when you enter Play Mode, and you will instead get vanilla Unity rendering.

- **Volume Camera**: A new component which defines what content within a Sim scene that should be displayed on the Host. A volume camera consists of a mode, an oriented bounding box (OBB), and a culling mask. There are currently two modes:
    - Bounded Mode: In this mode, all content within the volume camera's OBB *and* whose layer matches the culling mask will be replicated to the host. Content that falls on the border (partially inside and partially outside the OBB) will be clipped. 
    - Unbounded Mode: In this mode, the OBB is ignored, and all content in the scene whose layer matches the culling mask will be replicated to the host. No content is explicitly clipped. See (see [VolumeCamera](VolumeCamera.md)) for more details.

- **Exclusive Mode**: Refers to the runtime behavior where an app is the only active and visible application

- **Shared Mode**: Refers to the runtime behavior where other apps may be active and/or visible
