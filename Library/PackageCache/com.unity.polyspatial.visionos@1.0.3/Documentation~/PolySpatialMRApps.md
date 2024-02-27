---
uid: psl-vos-mr-apps
---
# PolySpatial Mixed Reality apps on visionOS
<a name="modes-and-volumes"></a>
Mixed Reality content on visionOS can be in one of two modes, which we refer to as "shared" and "exclusive" mode.

| **Modes** | **Description** |
| --- | --- |
| Shared | In "shared" mode, your application coexists with any other applications that are active in the shared real-world space. Each application has one or more **bounded volumes** (see below), but no unbounded volumes. The position and orientation of these volumes (both relative and absolute) is opaque to the app. Input in this mode is limited to a “3D touch” mechanism, via the **SpatialPointerDevice** (see [Input](Input.md)). In addition, ARKit information such as hand position, planes, or world mesh is unavailable in this mode. |
| Exclusive | In "exclusive" mode, a single application controls the entire view, via an **unbounded volume** (see below) in addition to previously created bounded volumes. In this mode, an app knows the relative positioning of its volumes, can access all AR features of the device, and use hand/joint position information to drive input and interactions directly. The app still does not have the ability to move or size bounded volumes, and thus must rely on the user to ensure bounded volumes don't overlap with meaningful content within the unbounded volume. |


## Volumes
<a name="volumes"></a>
Volumes are a new concept for mixed reality platforms. An application can create one or more volumes for displaying content in the mixed reality space. Each volume is an oriented box that contains 3D content. In visionOS, volumes can be moved and scaled in real-world space independently by the user, but not programmatically by the developer. In Unity you can interact with Volumes using a Unity component called a [VolumeCamera](VolumeCamera.md).

| **Modes** | **Description** |
| --- | --- |
| Bounded Volumes | Bounded volumes have a finite, box-shaped extent. Bounded volumes can be moved and transformed in world space by the user, but not programmatically by the developer. Currently, Unity content within a bounded volume will expand to fill the actual size of the volume.<br><br>Input in bounded volumes is limited to “3D Touch” as provided by the SpatialPointerDevice. See [Input](Input.md). |
| Unbounded Volumes | When running in exclusive mode, content presents a single unbounded volume, without any clipping edges. The application owns the entire mixed reality view, with no other applications visible. Additional bounded volumes from the same application can co-exist with this unbounded volume.<br><br>Within the unbounded volume, an application can request access to full hand tracking data. |

