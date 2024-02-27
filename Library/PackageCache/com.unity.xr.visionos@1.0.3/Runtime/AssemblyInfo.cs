using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.XR.VisionOS.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.XR.VisionOS.Editor")]
#endif
