using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using CreaJuego.Web;

namespace CreaJuego.Web.Editor
{
    public static class WebCompatibleBuild
    {
        const string RequestPath = "Library/CreaJuegoWebBuild.request";
        const string ResultPath = "Library/CreaJuegoWebBuild.result";
        // These files are copied after Unity generates the PWA shell so workshop branding survives every build.
        const string BrandingDirectory = "Assets/CreaJuegoWeb/Content/Branding";

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

        [MenuItem("CreaJuego/Web/Actualizar marca del taller")]
        public static void RefreshWorkshopBranding()
        {
            const string outputPath = @"E:\Github\creajuego-web";
            ApplyBranding(Path.Combine(outputPath, "index.html"), Path.Combine(outputPath, "manifest.webmanifest"));
            BumpServiceWorkerCache(outputPath);
            Debug.Log("CREAJUEGO_WEB_BRANDING_READY " + outputPath);
        }

        public static void Build(string outputPath)
        {
            outputPath = Path.GetFullPath(outputPath ?? string.Empty);
            if (!Directory.Exists(outputPath)) throw new DirectoryNotFoundException(outputPath);

            var indexPath = Path.Combine(outputPath, "index.html");
            var manifestPath = Path.Combine(outputPath, "manifest.webmanifest");
            if (!File.Exists(indexPath) || !File.Exists(manifestPath))
                throw new InvalidOperationException("La salida debe ser el repositorio PWA de CreaJuego Web.");

            var index = File.ReadAllText(indexPath);
            if (!index.Contains("<title>") || !index.Contains("</title>") ||
                !index.Contains("width=\"100%\"") || !index.Contains("height=\"100%\""))
                throw new InvalidOperationException("El index personalizado no contiene un título o el canvas al 100% esperado.");

            ApplyBranding(indexPath, manifestPath);

            if (!File.Exists(Path.GetFullPath(WebSpikeBuilder.ScenePath)))
                throw new FileNotFoundException("No existe la escena Web preparada.", WebSpikeBuilder.ScenePath);
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
            // Brotli minimizes the initial transfer for workshop locations with slow internet.
            // The fallback keeps the PWA compatible with static hosting such as GitHub Pages.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
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
                UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
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
            var cacheName = $"CreaJuego-Web-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.WriteAllText(path, UpgradeServiceWorker(File.ReadAllText(path), cacheName));
        }

        // Unity's PWA template normally searches every Cache Storage entry. That can mix
        // loader/framework/data/wasm files from different builds and execute incompatible code.
        // Keep lookups scoped to this build and remove previous CreaJuego caches on activation.
        public static string UpgradeServiceWorker(string text, string cacheName)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("El Service Worker está vacío.", nameof(text));
            if (string.IsNullOrWhiteSpace(cacheName)) throw new ArgumentException("La caché necesita un nombre.", nameof(cacheName));

            if (!text.Contains("TemplateData/biblored-logo.svg"))
                text = text.Replace("\"TemplateData/style.css\"", "\"TemplateData/style.css\",\n    \"TemplateData/biblored-logo.svg\",\n    \"TemplateData/gino-icon.png\"");

            text = Regex.Replace(text, @"^const cacheName\s*=\s*[^;]+;", $"const cacheName = \"{cacheName}\";", RegexOptions.Multiline);
            if (!text.Contains("self.skipWaiting()"))
                text = text.Replace(
                    "self.addEventListener('install', function (e) {\n    console.log('[Service Worker] Install');",
                    "self.addEventListener('install', function (e) {\n    console.log('[Service Worker] Install');\n    self.skipWaiting();");
            text = Regex.Replace(text, @"(?:\s*self\.skipWaiting\(\);){2,}", "\n    self.skipWaiting();");

            if (!text.Contains("self.addEventListener('activate'"))
            {
                const string activate = @"
self.addEventListener('activate', function (e) {
    e.waitUntil((async function () {
      const names = await caches.keys();
      await Promise.all(names
        .filter(name => name.startsWith('CreaJuego-Web-') && name !== cacheName)
        .map(name => caches.delete(name)));
      await self.clients.claim();
    })());
});

";
                text = text.Replace("self.addEventListener('fetch', function (e) {", activate + "self.addEventListener('fetch', function (e) {");
            }

            text = text.Replace(
                "let response = await caches.match(e.request);",
                "const cache = await caches.open(cacheName);\n      let response = await cache.match(e.request);");
            text = text.Replace(
                "response = await fetch(e.request);\n      const cache = await caches.open(cacheName);",
                "response = await fetch(e.request);");
            return text;
        }

        static void ApplyBranding(string indexPath, string manifestPath)
        {
            var pack = AssetDatabase.LoadAssetAtPath<RuntimeContentPack>("Assets/CreaJuegoWeb/Content/WebRuntimePack.asset");
            var product = !string.IsNullOrWhiteSpace(pack != null ? pack.productName : null) ? pack.productName : CreaJuegoBranding.ProductName;
            var tagline = !string.IsNullOrWhiteSpace(pack != null ? pack.tagline : null) ? pack.tagline : CreaJuegoBranding.Tagline;
            var fullTitle = product + ": " + tagline;
            var templateDirectory = Path.Combine(Path.GetDirectoryName(indexPath), "TemplateData");
            Directory.CreateDirectory(templateDirectory);

            var logoSource = Path.GetFullPath(BrandingDirectory + "/biblored-logo.svg");
            if (!File.Exists(logoSource)) throw new FileNotFoundException("Falta el logo oficial de BibloRed.", logoSource);
            File.Copy(logoSource, Path.Combine(templateDirectory, "biblored-logo.svg"), true);

            var playerIcon = pack != null ? pack.CategoryFor(ItemKind.Player)?.Default?.Preview : null;
            if (playerIcon == null) throw new InvalidOperationException("No se encontró la apariencia predeterminada de Gino para el favicon.");
            ExportSquareIcon(playerIcon, Path.Combine(templateDirectory, "gino-icon.png"), 144, 14);

            var index = File.ReadAllText(indexPath);
            index = Regex.Replace(index, "<title>.*?</title>", "<title>" + fullTitle + "</title>", RegexOptions.Singleline);
            index = Regex.Replace(index, "<link rel=\"shortcut icon\"[^>]*>", "<link rel=\"icon\" type=\"image/png\" href=\"TemplateData/gino-icon.png\">");
            File.WriteAllText(indexPath, index);

            var manifest = File.ReadAllText(manifestPath);
            manifest = Regex.Replace(manifest, "(\"name\"\\s*:\\s*\")[^\"]*(\")", "$1" + fullTitle + "$2", RegexOptions.None, TimeSpan.FromSeconds(1));
            manifest = Regex.Replace(manifest, "(\"short_name\"\\s*:\\s*\")[^\"]*(\")", "$1" + product + "$2", RegexOptions.None, TimeSpan.FromSeconds(1));
            manifest = Regex.Replace(manifest, "\"src\"\\s*:\\s*\"[^\"]+\"", "\"src\": \"TemplateData/gino-icon.png\"");
            manifest = Regex.Replace(manifest, "\"type\"\\s*:\\s*\"image/[^\"]+\"", "\"type\": \"image/png\"");
            File.WriteAllText(manifestPath, manifest);

            var stylePath = Path.Combine(templateDirectory, "style.css");
            var style = File.ReadAllText(stylePath);
            style = Regex.Replace(style, @"#unity-logo\s*\{[^}]*\}",
                "#unity-logo { width: 342px; height: 90px; background: url('biblored-logo.svg') no-repeat center; background-size: contain; }", RegexOptions.Singleline);
            if (!style.Contains("#unity-loading-bar::after"))
                style += "\n#unity-loading-bar::after { content: '" + fullTitle.Replace("'", "\\'") + "'; display: block; margin-top: 18px; color: #fff; font: 600 18px Arial, sans-serif; text-align: center; letter-spacing: .2px; }\n";
            File.WriteAllText(stylePath, style);
        }

        static void ExportSquareIcon(Sprite sprite, string outputPath, int size, int padding)
        {
            var preview = new PreviewRenderUtility();
            try
            {
                var visual = new GameObject("Gino Web Icon");
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;
                preview.AddSingleGO(visual);
                preview.camera.orthographic = true;
                preview.camera.clearFlags = CameraClearFlags.Color;
                preview.camera.backgroundColor = Color.clear;
                var bounds = renderer.bounds;
                var margin = size / Mathf.Max(1f, size - padding * 2f);
                preview.camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x) * margin;
                preview.camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
                preview.camera.transform.rotation = Quaternion.identity;
                preview.BeginStaticPreview(new Rect(0, 0, size, size));
                preview.camera.Render();
                var icon = preview.EndStaticPreview();
                File.WriteAllBytes(outputPath, icon.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(icon);
            }
            finally { preview.Cleanup(); }
        }
    }
}
