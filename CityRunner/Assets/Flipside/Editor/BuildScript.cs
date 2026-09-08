using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Flipside.EditorTools
{
    /// <summary>WebGL build entry point, usable from the menu or from the command line in batch mode.</summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/Flipside/Scenes/CityRun.unity";

        [MenuItem("Flipside/Build WebGL")]
        public static void BuildWebGL()
        {
            string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "WebGL");
            Build(output);
        }

        /// <summary>Called from batch mode: -executeMethod Flipside.EditorTools.BuildScript.BuildWebGLBatch -buildPath <dir></summary>
        public static void BuildWebGLBatch()
        {
            string output = null;
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-buildPath") output = args[i + 1];
            }
            if (string.IsNullOrEmpty(output)) output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "WebGL");
            BuildReport report = Build(output);
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        static BuildReport Build(string output)
        {
            PlayerSettings.productName = "FLIPSIDE: City Lights";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // plays from any static host
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("WebGL build " + report.summary.result + " -> " + output + " (" + report.summary.totalSize / (1024 * 1024) + " MB, " + report.summary.totalErrors + " errors)");
            return report;
        }
    }
}
