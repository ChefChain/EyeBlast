# FlatSharp Support
This package was built from a [Unity fork of FlatSharp](https://github.com/Unity-Technologies/FlatSharp)

It provides two main pieces of functionality:

1. A compiler to generate language specific bindings from a flatbuffer schema file.

2. A runtime to support C#/.NET bindings.

## FlatSharp.Compiler
The [compiler](Tools~/FlatSharp.Compiler/FlatSharp.Compiler.dll) and can be run using a local .NET 6 `dotnet` command. 

For Unity support, a path to a `UnityEngine.dll` must be provided:

```
dotnet Tools~/FlatSharp.Compiler/FlatSharp.Compiler.dll --input Tests/Runtime/test.fbs --output Tests/Runtime --unity-assembly-path=<local editor install>/Unity.app/Contents/Managed/UnityEngine/UnityEngine.CoreModule.dll
```

## Runtime

Runtime support is provided by a prebuilt [FlatSharp.Runtime.dll](Packages/com.unity.ext.flatsharp/Runtime/FlatSharp.Runtime.dll) and an asmdef containing [Unity specific extensions](Packages/com.unity.ext.flatsharp/Runtime/UnityExtensions.cs) needed to support `NativeArray` as a vector option.

## FlatC Compiler

The `flatc` binaries in `Tools~/FlatSharp.Compiler/flatc` are custom built from a Unity fork with details [here](Tools~/FlatSharp.Compiler/flatc/flatc.version.txt).

## Swift Support

Swift support is copied directly from the same [custom Unity fork of flatbuffers](https://github.com/Unity-Technologies/flatbuffers) and revision as the flatc compiler above.

## Updating

### flatc

1. Make PRs into `unity-master` branch on https://github.com/Unity-Technologies/flatbuffers.

2. Open Yamato and navigate to the [flatbuffers project](https://unity-ci.cds.internal.unity3d.com/project/2197).

3. Select the `unity-master` branch (or whatever appropriate branch) and trigger the "Build All" job.

4. Download the artifacts from the completed "Build All" job and place them in the appropriate directories under `Tools\~/FlatSharp.Compiler/flatc`. (TODO: lipo the mac binaries instead of just using the x64 one.)

5. On Mac, run `xattr -r -d com.apple.quarantine Tools~/FlatSharp.Compiler/flatc` to clear out any download quarantine flags so you can test locally.

6. Update `Tools~/FlatSharp.Compiler/flatc/flatc.version.txt` with the commit hash.

### Swift support

1. From the same branch you generated `flatc` from above, copy the contents of the `swift` directory and `*.swift` to `Src~/flatbuffers` in this package.

### FlatSharp

1. TODO

