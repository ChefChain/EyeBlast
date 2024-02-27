using System.IO;
using UnityEngine;

namespace UnityEditor.PolySpatial.Internals.InternalBridge
{
    /// <summary>
    /// Internal access is required here to access to the WindowLayout class and methods.
    /// However, this access to internal methods using the bridge should ideally be only temporary and don't last over time.
    /// </summary>
    [InitializeOnLoad]
    static class PolySpatialLayoutUtils
    {
        const string k_LoadedKey = "PolySpatial/LayoutLoaded";
        static bool Loaded => EditorPrefs.GetBool(k_LoadedKey, false);

        const string k_PathToResources = "Packages/com.unity.polyspatial/Editor/Resources/";
        static string widgetsWorkspace => Path.Combine(k_PathToResources,"widgets-workspace.wlt");
        static string xrWorkspace => Path.Combine(k_PathToResources+"xr-workspace.wlt");

        const string k_WidgetsLayoutName = "Widget Workspace.wlt";
        const string k_XrWorkspaceLayoutName = "XR Workspace.wlt";

        static PolySpatialLayoutUtils()
        {
            if (!Loaded)
                LoadInitialLayout();
        }

        //Loading the default PolySpatial layout if it has never been loaded in the project before.
        static void LoadInitialLayout()
        {
            if (AddPolySpatialLayouts())
            {
                var path = FileUtil.CombinePaths(WindowLayout.layoutsModePreferencesPath, k_WidgetsLayoutName);
                EditorUtility.LoadWindowLayout(path);
            }
            SessionState.SetBool(k_LoadedKey, true);
        }

        //Adding the PolySpatial Layout to the menu list of the available layouts
        //returns true if the layouts are newly added to load the default one after,
        // false if they are replacing already existing ones, so that we don't remove the User workspace
        static bool AddPolySpatialLayouts()
        {
            var newLayout = true;

            var layoutsModePreferencesPath = WindowLayout.layoutsModePreferencesPath;
            if (!Directory.Exists(layoutsModePreferencesPath))
                Directory.CreateDirectory(layoutsModePreferencesPath);

            var path = FileUtil.CombinePaths(layoutsModePreferencesPath, k_WidgetsLayoutName);
            if (File.Exists(path))
            {
                newLayout = false;
                File.Delete(path);
            }
            File.Copy(Path.GetFullPath(widgetsWorkspace), path);

            path = FileUtil.CombinePaths(layoutsModePreferencesPath, k_XrWorkspaceLayoutName);
            if (File.Exists(path))
            {
                newLayout = false;
                File.Delete(path);
            }
            File.Copy(Path.GetFullPath(xrWorkspace), path);

            if(newLayout)
                WindowLayout.ReloadWindowLayoutMenu();

            return newLayout;
        }
    }
}
