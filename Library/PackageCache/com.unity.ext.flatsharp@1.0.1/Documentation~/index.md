---
uid: flatsharp-index
---
# FlatSharp Support
This package was built from a [Unity fork of FlatSharp](https://github.com/Unity-Technologies/FlatSharp)

It provides two main pieces of functionality:

1. A compiler to generate language specific bindings from a flatbuffer schema file.
2. A runtime to support C#/.NET bindings.

## FlatSharp.Compiler
The compiler, located in the package source at `Tools~/FlatSharp.Compiler/FlatSharp.Compiler.dll`, can be run using a local .NET 6 `dotnet` command. 

For Unity support, a path to a `UnityEngine.dll` must be provided:

```
dotnet Tools~/FlatSharp.Compiler/FlatSharp.Compiler.dll --input Tests/Runtime/test.fbs --output Tests/Runtime --unity-assembly-path=<local editor install>/Unity.app/Contents/Managed/UnityEngine/UnityEngine.CoreModule.dll
```

## Runtime

Runtime support is provided by a prebuilt FlatSharp.Runtime.dll (in Packages/com.unity.ext.flatsharp/Runtime) and an asmdef containing Unity specific extensions (Packages/com.unity.ext.flatsharp/Runtime/UnityExtensions.cs) needed to support `NativeArray` as a vector option.

## FlatC Compiler

The `flatc` binaries in `Tools~/FlatSharp.Compiler/flatc` are custom built as described in the `Tools~/FlatSharp.Compiler/flatc/flatc.version.txt` file in the package source.

## Swift Support

Swift support is copied directly from the same [custom Unity fork of flatbuffers](https://github.com/Unity-Technologies/flatbuffers) and revision as the flatc compiler above.
