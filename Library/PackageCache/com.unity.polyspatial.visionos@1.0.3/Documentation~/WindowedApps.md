---
uid: psl-vos-windowed-apps
---
# Windowed Apps in visionOS
In visionOS, users can use [windows to present 2D or 3D content](https://developer.apple.com/design/human-interface-guidelines/windows#visionOS), or use volumes to present 3D content and objects. Unity describes such applications that live within these windows as **Windowed Apps**.

Windowed Apps use Unity's own rendering pipeline, either the Built-in Render Pipeline or Universal Render Pipeline. This gives you access to all the standard Unity features, but not the visionOS-specific features like stereo rendering and hover effects, nor AR-specific features such as joint tracking and anchors.

By default, your Unity content will become Windowed Apps in visionOS if you build a Unity application targeted at the visionOS platform without enabling the PolySpatial Runtime or visionOS plugin via the XR Plugin Manager. 

If you have the visionOS plugin enabled, but you still want to make a Windowed App, you must change the app mode under `Project Settings > XR Plug-in Management > Apple visionOS > App Mode` to `Windowed - 2D Window`.

To detect interactions on the Unity application within a window component on visionOS, you can use “Touch Support” provided by the input system package ([com.unity.inputsystem](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.6/manual/Touch.html)). visionOS limits touch input to two touch points. To learn more about designing for the windows component on visionOS, [visit Apple’s Human Interface Guidelines for visionOS. ](https://developer.apple.com/design/human-interface-guidelines/windows#visionOS)
