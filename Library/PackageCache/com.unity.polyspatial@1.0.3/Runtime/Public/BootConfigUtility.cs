using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Device;
using Application = UnityEngine.Application;

namespace Unity.PolySpatial.Internals
{
    internal class BootConfigUtility
    {
        List<(string, string)> bootConfigEntries = new();

        BootConfigUtility()
        {
#if !UNITY_EDITOR
            ReadPlayerBootConfig();
#else
            throw new InvalidOperationException("This constructor should only be used in player builds");
#endif
        }

        static Lazy<BootConfigUtility> s_Instance = new Lazy<BootConfigUtility>(() => { return new BootConfigUtility(); });

        public static BootConfigUtility Runtime => s_Instance.Value;

        // This is the runtime (in-player) built version. Unfortunately
        // there is no way to get access to bootconfig from the player itself, so we have
        // to hardcode the expected paths here.
        void ReadPlayerBootConfig()
        {
            #if UNITY_STANDALONE_OSX
            var path = Path.Combine(Application.dataPath, "Resources", "Data", "boot.config");
            #else
            // Standard location; the OSX dataPath is probably a bug
            var path = Path.Combine(Application.dataPath, "boot.config");
            #endif

            ReadBootConfig(path);
        }

        public BootConfigUtility(string bootConfigPath)
        {
            ReadBootConfig(bootConfigPath);
        }

        void ReadBootConfig(string bootConfigPath)
        {
            if (!File.Exists(bootConfigPath))
            {
                UnityEngine.Debug.LogError($"Boot config file {bootConfigPath} does not exist");
                throw new InvalidOperationException($"Boot config file {bootConfigPath} does not exist");
            }

            var bootConfig = File.ReadAllText(bootConfigPath);
            var lines = bootConfig.Split('\n');
            foreach (var l in lines)
            {
                var line = l.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.StartsWith("#"))
                {
                    bootConfigEntries.Add((line, null));
                    continue;
                }

                var equals = line.IndexOf("=", StringComparison.InvariantCulture);
                string k;
                string v = null;
                if (equals == -1)
                {
                    k = line;
                }
                else
                {
                    k = line.Substring(0, equals);
                    v = line.Substring(equals + 1);
                }

                bootConfigEntries.Add((k, v));
            }
        }

        public override string ToString()
        {
            StringBuilder bootConfig = new();
            foreach (var (key, value) in bootConfigEntries)
            {
                if (value == null)
                {
                    bootConfig.Append(key);
                }
                else
                {
                    bootConfig.Append($"{key}={value}");
                }
                bootConfig.Append("\n");
            }

            return bootConfig.ToString();
        }

        public string GetValue(string key)
        {
            for (int i = 0; i < bootConfigEntries.Count; i++)
            {
                var (k, v) = bootConfigEntries[i];
                if (k == key)
                {
                    return v;
                }
            }

            return null;
        }

        // Writing only makes sense in the editor
        [Conditional("UNITY_EDITOR")]
        public void SetValue(string key, string value)
        {
            for (int i = 0; i < bootConfigEntries.Count; i++)
            {
                var (k, v) = bootConfigEntries[i];
                if (k == key)
                {
                    bootConfigEntries[i] = (k, value);
                    return;
                }
            }

            bootConfigEntries.Add((key, value));
        }
    }
}
