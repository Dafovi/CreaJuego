using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CreaJuego.Web.Editor
{
    public static class WebCompatibleBuild
    {
        const string RequestPath = "Library/CreaJuegoWebBuild.request";
        const string ResultPath = "Library/CreaJuegoWebBuild.result";

        [InitializeOnLoadMethod]
        static void RunRequestedBuild()
        {
            var request = Path.GetFullPath(RequestPath);
            if (!File.Exists(request)) return;
            var output = File.ReadAllText(request).Trim();
            File.Delete(request);
            EditorApplication.delayCall += () =>
            {
                try
                {
                    Build(output);
                    File.WriteAllText(ResultPath, "SUCCESS\n" + output);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    File.WriteAllText(ResultPath, "FAILED\n" + exception);
                }
            };
        }

        [MenuItem("CreaJuego/Web/Generar Web compatible en repositorio")]
        public static void BuildFromMenu() => Build(@"E:\Github\creajuego-web");

        public static void Build(string outputPath)
        {
            outputPath = Path.GetFullPath(outputPath ?? string.Empty);
            if (!Directory.Exists(outputPath)) throw new DirectoryNotFoundException(outputPath);

            var indexPath = Path.Combine(outputPath, "index.html");
            var manifestPath = Path.Combine(outputPath, "manifest.webmanifest");
            if (!File.Exists(indexPath) || !File.Exists(manifestPath))
                throw new InvalidOperationException("La salida debe ser el repositorio PWA de CreaJuego Web.");

            var index = File.ReadAllText(indexPath);
            if (!index.Contains("<title>CreaJuego Web | Dafovi_LabCo</title>") ||
                !index.Contains("width=\"100%\" height=\"100%\""))
                throw new InvalidOperationException("El index personalizado no contiene el título o el canvas al 100% esperados.");

            WebSpikeBuilder.Prepare();
            Configure();

            var stagingRoot = Path.Combine(outputPath, ".unity-staging", "creajuego-web");
            if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, true);
            Directory.CreateDirectory(stagingRoot);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { WebSpikeBuilder.ScenePath },
                locationPathName = stagingRoot,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report == null || report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("WebGL compatible falló: " + (report == null ? "sin reporte" : report.summary.result.ToString()));

            ReplaceDirectory(Path.Combine(stagingRoot, "Build"), Path.Combine(outputPath, "Build"));
            var stagedStreaming = Path.Combine(stagingRoot, "StreamingAssets");
            if (Directory.Exists(stagedStreaming))
                ReplaceDirectory(stagedStreaming, Path.Combine(outputPath, "StreamingAssets"));

            BumpServiceWorkerCache(outputPath);
            Directory.Delete(Path.Combine(outputPath, ".unity-staging"), true);
            Debug.Log($"CREAJUEGO_WEB_COMPATIBLE_READY {report.summary.totalSize} bytes -> {outputPath}");
        }

        static void Configure()
        {
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.defaultScreenWidth = 960;
            PlayerSettings.defaultScreenHeight = 540;
            PlayerSettings.defaultWebScreenWidth = 960;
            PlayerSettings.defaultWebScreenHeight = 540;

            PlayerSettings.WebGL.template = "APPLICATION:PWA";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 512;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.geometricMemoryGrowthStep = .2f;
            PlayerSettings.WebGL.memoryGeometricGrowthCap = 96;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.wasm2023 = false;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.Default;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);

            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
            UnityEditor.WebGL.UserBuildSettings.codeOptimization =
                UnityEditor.WebGL.WasmCodeOptimization.RuntimeSpeedLTO;
            AssetDatabase.SaveAssets();
        }

        static void ReplaceDirectory(string source, string destination)
        {
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(directory.Replace(source, destination));
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(source, destination), true);
        }

        static void BumpServiceWorkerCache(string outputPath)
        {
            var path = Path.Combine(outputPath, "ServiceWorker.js");
            if (!File.Exists(path)) return;
            var text = File.ReadAllText(path);
            var firstLineEnd = text.IndexOf('\n');
            if (firstLineEnd < 0) return;
            File.WriteAllText(path,
                $"const cacheName = \"CreaJuego-Web-{DateTime.UtcNow:yyyyMMddHHmmss}\";\n" +
                text.Substring(firstLineEnd + 1));
        }
    }
}




