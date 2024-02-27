using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using AOT;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor;

namespace Unity.PolySpatial.Internals
{
    internal static class RKRuntimeFuncs
    {
        delegate void GetPolySpatialNativeApiFn(out Platform.PolySpatialNativeAPI api);

        delegate void GenericFunctionDelegate();

        static GetPolySpatialNativeApiFn GetPolySpatialNativeAPI_dynamic;

        static bool s_initDone;
        static IntPtr s_libHandle;

#if UNITY_VISIONOS
        const string kPluginName = "__Internal"; // on iOS, it's a static library linked with the il2cpp code
#else
        const string kPluginName = "PolySpatialNotSupportedStaticDllImportPlatform";
#endif

        [DllImport(kPluginName, EntryPoint = "GetPolySpatialNativeAPI")]
        private static extern void GetPolySpatialNativeAPI_static(out Platform.PolySpatialNativeAPI api);

        internal static void GetPolySpatialNativeAPI(out Platform.PolySpatialNativeAPI api)
        {
            SetupHandles();
            if (GetPolySpatialNativeAPI_dynamic != null)
                GetPolySpatialNativeAPI_dynamic(out api);
            else
                GetPolySpatialNativeAPI_static(out api);
        }

        internal static void SetupHandles()
        {
            if (s_initDone)
                return;

            s_initDone = true;

            string libpath = Environment.GetEnvironmentVariable("RK_RUNTIME_PATH");

            if (String.IsNullOrEmpty(libpath))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var dotrk = Path.Combine(home, ".rkpath");

                if (File.Exists(dotrk))
                {
                    libpath = File.ReadAllText(dotrk).Trim();
                }
            }

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (String.IsNullOrEmpty(libpath))
            {
                // a native player build on mac.  We put our .bundle in the PlugIns directory,
                // but Unity doesn't load it through a plugin asset.  Note weird PlugIns capitalization
                // is a Unity quirk.

                // just in case we're directly linked with the bundle
                if (TryLoadFunctionPointers(IntPtr.Zero))
                    return;

                var newlibpath = Path.Combine(Application.dataPath, "PlugIns", "PolySpatial-macOSNew.bundle", "Contents", "MacOS", "PolySpatial-macOSNew");
                var oldlibpath = Path.Combine(Application.dataPath, "PlugIns", "PolySpatial-macOS.bundle", "Contents", "MacOS", "PolySpatial-macOS");
                libpath = File.Exists(newlibpath) ? newlibpath
                    : File.Exists(oldlibpath) ? oldlibpath
                    : null;

                if (libpath == null)
                {
                    Debug.LogError($"Expected mac plugin {newlibpath} or {oldlibpath} doesn't exist");
                }
            }
#endif

            if (String.IsNullOrEmpty(libpath))
                return;

            // Call dlerror() to make sure it gets loaded before we need to call it.. otherwise
            // it'll do a dlopen() itself on the first call and wipe away the error we want.
            dlerror();

            s_libHandle = dlopen(libpath, RTLD_LOCAL | RTLD_FIRST);
            if (s_libHandle == IntPtr.Zero)
            {
                Debug.LogError($"Trying to dlopen(\"{libpath}\") failed with error:\n{dlerror_string()}");
                return;
            }

            //Debug.LogWarning($"RKRuntimeFuncs dlopen() -> {s_libHandle}");
            if (!TryLoadFunctionPointers(s_libHandle))
            {
                throw new InvalidOperationException(
                    $"Couldn't find GetPolySpatialNativeAPI symbols from library path {libpath}");
            }

#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
#endif
        }

        internal static void Shutdown()
        {
            if (s_libHandle != IntPtr.Zero && s_libHandle != new IntPtr(-1))
            {
                //Debug.LogWarning($"RKRuntimeFuncs dlclose({s_libHandle})");
                dlclose(s_libHandle);
            }

            s_libHandle = IntPtr.Zero;
        }

        static bool TryLoadFunctionPointers(IntPtr libHandle)
        {
            var fn1 = dlsym(libHandle, "GetPolySpatialNativeAPI");

            if (fn1 == IntPtr.Zero)
            {
                return false;
            }

            GetPolySpatialNativeAPI_dynamic = Marshal.GetDelegateForFunctionPointer<GetPolySpatialNativeApiFn>(fn1);
            return true;
        }

        const int RTLD_LOCAL = 0x0001;
        const int RTLD_NOW = 0x0002;
        const int RTLD_LAZY = 0x0004;
        const int RTLD_GLOBAL = 0x0080;
        const int RTLD_NOLOAD = 0x10;
        const int RTLD_NODELETE = 0x80;
        const int RTLD_FIRST = 0x100;

        const string DYLD_LIB_NAME = "dl";

        [DllImport(DYLD_LIB_NAME)]
        internal static extern IntPtr dlopen(string path, int flags);

        [DllImport(DYLD_LIB_NAME)]
        internal static extern void dlclose(IntPtr handle);

        [DllImport(DYLD_LIB_NAME)]
        internal static extern IntPtr dlsym(IntPtr handle, string fname);

        [DllImport(DYLD_LIB_NAME)]
        internal static extern IntPtr dlerror();

        static string dlerror_string()
        {
            var errp = dlerror();
            if (errp == IntPtr.Zero)
                return null;
            return Marshal.PtrToStringAnsi(errp);
        }
    }
}
