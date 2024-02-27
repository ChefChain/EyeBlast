---
uid: psl-vos-tutorial-new-project
---
# Starting a new visionOS project from scratch

This page describes how to start a project from scratch using one or more of the available modes.

# Requirements

Before starting, ensure you meet the [Hardware and Software Requirements](Requirements.md).

<a name="starting-a-new-visionos-mr-project-from-scratch"></a>


## Windowed App

1. Open the **Build Settings** window (menu: **File &gt; Build Settings**).
2. Select the **visionOS** platform.
3. If necessary, click **Switch Platform** to change to the visionOS platform.
4. Add and select any Scenes you want to include in the build. (For example, SampleScene.)
5. Click the **Build** button.

By default, Unity builds that target visionOS will be set up to run in windowed mode. If you install XR or PolySpatial support (by following steps 1-8 from **Fully Immersive Virtual Reality** below), you need to manually configure your App Mode in order to build and deploy a 2D windowed application:

1. Open Project Settings.
2. Change the app mode under `XR Plug-in Management > Apple visionOS > App Mode` to `Windowed - 2D Window`.


Windowed Apps use Unity's own rendering pipeline, such as the Built-in Render Pipeline or Universal Render Pipeline. See [Windowed Apps](WindowedApps.md) for details.

## Fully Immersive Virtual Reality

1. Open the **Project Settings** window (menu:**Edit &gt; Project Settings**).
2. Select the **XR Plug-in Management** section.
3. If necessary, click the button to **Install XR Plug-in Management**.
4. Select the tab for the **visionOS** target build platform.
5. Enable the **Apple visionOS** Plug-in Provider.
6. Select the **Apple visionOS** settings section under **XR Plug-in Management**.
7. Set the **App Mode** to **Virtual Reality - Fully Immersive Space**.
8. Set the **Target SDK** to run on the device or simulator.
    1. Open the **Project Settings** window (menu: **Edit &gt; Project Settings**) and select the **Player** section.
    2. Under **Other Settings &gt; Configuration**, set **Target SDK** to **Device SDK** to run on the Apple Vision Pro device or **Simulator SDK** to run on the simulator.
9. Open the **Build Settings** window (menu: **File &gt; Build Settings**).
    1. Select the **visionOS** platform.
    2. If necessary, click **Switch Platform** to change to the visionOS platform.
    3. Add and select any Scenes you want to include in the build. (For example, SampleScene.)
    4. Click the **Build** button.

Your app will render a full immersive space and you should see the Unity skybox (or your app) running in the Apple Vision Pro simulator.

Refer to [Fully Immersive VR](VRApps.md) docs for more information

## Mixed Reality and Shared Space

For bounded apps, your app can exist alongside other apps in the shared space. For unbounded apps, your app will be the only content visible.

1. Follow steps 1-8 from above, this time setting **App Mode** to **Mixed Reality - Volume or Immersive Space**.
    
    This should automatically install the required packages, **com.unity.polyspatial**, **com.unity.polyspatial.visionos**, and **com.unity.polyspatial.xr**.
2. Create a Volume Camera in your scene.
    1. From the **GameObject &gt; XR &gt; Setup** menu or the **XR Building Blocks** overlay, click **Volume Camera**.
    2. Add a **VolumeCameraWindowConfiguration** asset to your project with **Create &gt; PolySpatial &gt; Volume Camera Window Configuration**. You must store this asset in one of your project's **Resources** folders. (Refer to [Special Folders](xref:SpecialFolders) for more information about **Resources** folders.)
    3. Assign the volume camera window configuration to the **Volume Window Configuration** of the volume camera.
3. Configure the volume camera for bounded or unbounded mode and adjust the dimensions (if bounded).
    - Dimensions adjust the rendering scale of your content.
    - For bounded apps, make sure something is visible within the dimensions of the volume camera.
4. Open the **Build Settings** window (menu: **File &gt; Build Settings**).
    1. Select the **visionOS** platform.
    2. If necessary, click **Switch Platform** to change to the visionOS platform.
    3. Add and select any Scenes you want to include in the build. (For example, SampleScene.)
    4. Click the **Build** button.

**Unbounded apps**
For unbounded apps that use ARKit features, add the **com.unity.xr.arfoundation** package to your project. To use skeletal hand tracking data, add the **com.unity.xr.hands** package to your project. Refer to [XR packages](xref:xr-support-packages) for more information about Unity's XR packages.

> [!NOTE]
> The Apple Vision Pro simulator does not provide any ARKit data, so planes, meshes, tracked hands, etc. do not work in the simulator.

Refer to [PolySpatial MR Apps](PolySpatialMRApps.md) docs for more information