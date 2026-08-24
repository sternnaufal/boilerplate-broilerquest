using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Linq;

namespace BroilerQuest.Editor
{
    public static class AndroidBuildScript
    {
        private static readonly string[] Scenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/SelectLevel.unity",
            "Assets/Scenes/Starter.unity",
            "Assets/Scenes/Beginner.unity",
            "Assets/Scenes/Intermediate.unity",
            "Assets/Scenes/KoleksiIoT.unity"
        };

        [MenuItem("Build/Android AAB (Google Play)")]
        public static void BuildAndroidAAB()
        {
            string buildPath = EditorUtility.SaveFilePanel(
                "Save Android AAB",
                "",
                "BroilerQuest",
                "aab");

            if (string.IsNullOrEmpty(buildPath)) return;

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            // Force AAB mode for Google Play
            EditorUserBuildSettings.buildAppBundle = true;

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {summary.totalSize} bytes, {summary.totalTime}");
                EditorUtility.RevealInFinder(buildPath);
            }
            else
            {
                Debug.LogError($"Build failed: {summary.result}");
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error || message.type == LogType.Warning)
                            Debug.LogError($"  {message.content}");
                    }
                }
            }
        }

        [MenuItem("Build/Android APK (Debug)")]
        public static void BuildAndroidAPK()
        {
            string buildPath = EditorUtility.SaveFilePanel(
                "Save Android APK",
                "",
                "BroilerQuest_Debug",
                "apk");

            if (string.IsNullOrEmpty(buildPath)) return;

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            EditorUserBuildSettings.buildAppBundle = false;

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {summary.totalSize} bytes, {summary.totalTime}");
                EditorUtility.RevealInFinder(buildPath);
            }
            else
            {
                Debug.LogError($"Build failed: {summary.result}");
            }
        }

        [MenuItem("Build/Android APK (Release)")]
        public static void BuildAndroidAPKRelease()
        {
            string buildPath = EditorUtility.SaveFilePanel(
                "Save Android APK",
                "",
                "BroilerQuest",
                "apk");

            if (string.IsNullOrEmpty(buildPath)) return;

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            EditorUserBuildSettings.buildAppBundle = false;

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {summary.totalSize} bytes, {summary.totalTime}");
                EditorUtility.RevealInFinder(buildPath);
            }
            else
            {
                Debug.LogError($"Build failed: {summary.result}");
            }
        }
    }
}
