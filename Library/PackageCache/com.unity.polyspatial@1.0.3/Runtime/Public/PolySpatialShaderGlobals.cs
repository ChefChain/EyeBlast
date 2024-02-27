using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.PolySpatial
{
    public static class PolySpatialShaderGlobals
    {
        public const string Time = "_Time";
        public const string SinTime = "_SinTime";
        public const string CosTime = "_CosTime";
        public const string DeltaTime = "unity_DeltaTime";
        public const string WorldSpaceCameraPos = "_WorldSpaceCameraPos";
        public const string WorldSpaceCameraDir = "_WorldSpaceCameraDir";
        public const string OrthoParams = "unity_OrthoParams";
        public const string ProjectionParams = "_ProjectionParams";
        public const string ScreenParams = "_ScreenParams";
        public const string ViewMatrix = "UNITY_MATRIX_V";
        public const string ProjectionMatrix = "UNITY_MATRIX_P";
        public const string AmbientSkyColor = "unity_AmbientSky";
        public const string AmbientEquatorColor = "unity_AmbientEquator";
        public const string AmbientGroundColor = "unity_AmbientGround";

        public const int LightCount = 4;
        public const string LightColorPrefix = "polySpatial_LightColor";
        public const string LightPositionPrefix = "polySpatial_LightPosition";
        public const string SpotDirectionPrefix = "polySpatial_SpotDirection";
        public const string LightAttenPrefix = "polySpatial_LightAtten";

        public const string GlossyEnvironmentColor = "polySpatial_GlossyEnvironmentColor";

        internal static readonly int TimeID = Shader.PropertyToID(Time);
        internal static readonly int SinTimeID = Shader.PropertyToID(SinTime);
        internal static readonly int CosTimeID = Shader.PropertyToID(CosTime);
        internal static readonly int DeltaTimeID = Shader.PropertyToID(DeltaTime);
        internal static readonly int WorldSpaceCameraPosID = Shader.PropertyToID(WorldSpaceCameraPos);
        internal static readonly int WorldSpaceCameraDirID = Shader.PropertyToID(WorldSpaceCameraDir);
        internal static readonly int OrthoParamsID = Shader.PropertyToID(OrthoParams);
        internal static readonly int ProjectionParamsID = Shader.PropertyToID(ProjectionParams);
        internal static readonly int ScreenParamsID = Shader.PropertyToID(ScreenParams);
        internal static readonly int ViewMatrixID = Shader.PropertyToID(ViewMatrix);
        internal static readonly int ProjectionMatrixID = Shader.PropertyToID(ProjectionMatrix);
        internal static readonly int AmbientSkyColorID = Shader.PropertyToID(AmbientSkyColor);
        internal static readonly int AmbientEquatorColorID = Shader.PropertyToID(AmbientEquatorColor);
        internal static readonly int AmbientGroundColorID = Shader.PropertyToID(AmbientGroundColor);
        internal static readonly int GlossyEnvironmentColorID = Shader.PropertyToID(GlossyEnvironmentColor);

        internal static readonly int[] LightColorIDs = GetLightPropertyIDs(LightColorPrefix);
        internal static readonly int[] LightPositionIDs = GetLightPropertyIDs(LightPositionPrefix);
        internal static readonly int[] SpotDirectionIDs = GetLightPropertyIDs(SpotDirectionPrefix);
        internal static readonly int[] LightAttenIDs = GetLightPropertyIDs(LightAttenPrefix);

        public enum PropertyType
        {
            Float,
            Integer,
            Vector,
            Color,
            Matrix,
            Texture,
        }

        static Dictionary<string, PropertyType> s_PropertyTypes = new()
        {
            [Time] = PropertyType.Vector,
            [SinTime] = PropertyType.Vector,
            [CosTime] = PropertyType.Vector,
            [DeltaTime] = PropertyType.Vector,
            [WorldSpaceCameraPos] = PropertyType.Vector,
            [WorldSpaceCameraDir] = PropertyType.Vector,
            [OrthoParams] = PropertyType.Vector,
            [ProjectionParams] = PropertyType.Vector,
            [ScreenParams] = PropertyType.Vector,
            [ViewMatrix] = PropertyType.Matrix,
            [ProjectionMatrix] = PropertyType.Matrix,
            [AmbientSkyColor] = PropertyType.Vector,
            [AmbientEquatorColor] = PropertyType.Vector,
            [AmbientGroundColor] = PropertyType.Vector,
            [GlossyEnvironmentColor] = PropertyType.Vector,
        };

        static Dictionary<PropertyType, string[]> s_Names = new();

        static PolySpatialShaderGlobals()
        {
            for (var i = 0; i < LightCount; ++i)
            {
                s_PropertyTypes.Add(LightColorPrefix + i, PropertyType.Vector);
                s_PropertyTypes.Add(LightPositionPrefix + i, PropertyType.Vector);
                s_PropertyTypes.Add(SpotDirectionPrefix + i, PropertyType.Vector);
                s_PropertyTypes.Add(LightAttenPrefix + i, PropertyType.Vector);
            }
        }

        static int[] GetLightPropertyIDs(string prefix)
        {
            var propertyIDs = new int[LightCount];
            for (var i = 0; i < LightCount; ++i)
            {
                propertyIDs[i] = Shader.PropertyToID(prefix + i);
            }
            return propertyIDs;
        }

        internal static int GetCount()
        {
            return s_PropertyTypes.Count;
        }

        internal static string[] GetNames(PropertyType type)
        {
            if (s_Names.TryGetValue(type, out var names))
                return names;
            
            names = s_PropertyTypes
                .Where(entry => entry.Value == type)
                .Select(entry => entry.Key)
                .ToArray();

            s_Names.Add(type, names);

            return names;
        }

        /// <summary>
        /// Sets the value of a float shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetFloat(string name, float value)
        {
            if (TryAdd(name, PropertyType.Float))
                Shader.SetGlobalFloat(name, value);
        }

        /// <summary>
        /// Sets the value of an integer shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetInteger(string name, int value)
        {
            if (TryAdd(name, PropertyType.Integer))
                Shader.SetGlobalInteger(name, value);
        }

        /// <summary>
        /// Sets the value of a vector shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetVector(string name, Vector4 value)
        {
            if (TryAdd(name, PropertyType.Vector))
                Shader.SetGlobalVector(name, value);
        }

        /// <summary>
        /// Sets the value of a color shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetColor(string name, Color value)
        {
            if (TryAdd(name, PropertyType.Color))
                Shader.SetGlobalColor(name, value);
        }

        /// <summary>
        /// Sets the value of a matrix shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetMatrix(string name, Matrix4x4 value)
        {
            if (TryAdd(name, PropertyType.Matrix))
                Shader.SetGlobalMatrix(name, value);
        }

        /// <summary>
        /// Sets the value of a texture shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetTexture(string name, Texture value)
        {
            if (TryAdd(name, PropertyType.Texture))
                Shader.SetGlobalTexture(name, value);
        }

        /// <summary>
        /// Attempts to add a property to the list of shader globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to add.</param>
        /// <param name="type">The type of the shader global.</param>
        /// <returns>True if added or already present with the same type, false if already present
        /// with a different type (in which case an error will be logged).</returns>
        public static bool TryAdd(string name, PropertyType type)
        {
            if (!s_PropertyTypes.TryGetValue(name, out var currentType))
            {
                s_PropertyTypes.Add(name, type);

                // Force the list of names to be regenerated next time it is requested.
                s_Names.Remove(type); 
                
                return true;
            }
            if (currentType == type)
                return true;
            
            Debug.LogError($"Global {name} already exists with a different type: {currentType}.");
            return false;
        }
    }
}