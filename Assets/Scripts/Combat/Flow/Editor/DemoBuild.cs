using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

namespace Visha.Combat.Flow.EditorTools
{
    public static class DemoBuild
    {
        public static void BuildMac()
        {
            string path = Environment.GetEnvironmentVariable("VISHA_BUILD_PATH");
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Set VISHA_BUILD_PATH to the destination .app path.");
            var previousMode = PlayerSettings.fullScreenMode;
            int previousWidth = PlayerSettings.defaultScreenWidth;
            int previousHeight = PlayerSettings.defaultScreenHeight;
            bool previousNative = PlayerSettings.defaultIsNativeResolution;
            try
            {
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Campaign_Demo.unity" },
                locationPathName = path,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed: " + report.summary.result);
            }
            finally
            {
                PlayerSettings.fullScreenMode = previousMode;
                PlayerSettings.defaultScreenWidth = previousWidth;
                PlayerSettings.defaultScreenHeight = previousHeight;
                PlayerSettings.defaultIsNativeResolution = previousNative;
            }
        }
    }
}
