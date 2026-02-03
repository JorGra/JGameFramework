// JG Project Bootstrapper
// Provides a multi-step Unity Editor window to standardize project setup:
// - Initialize Git and Unity .gitignore
// - Add shared repos as submodules (Essentials, GameFramework)
// - Ensure required UPM dependencies
// - Install DOTween via UPM (Git mirror) and enforce asmdefs
//
// Safe to rerun; operations are idempotent where possible.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace JG.EditorTools
{
    public class JGProjectSetupWindow : EditorWindow
    {
        private const string EssentialsRepo = "https://github.com/JorGra/JG-UnityEssentials.git";
        private const string EssentialsPath = "Assets/JG-UnityEssentials";

        private const string EflatunId = "com.eflatun.scenereference";
        private const string EflatunUrl = "https://github.com/starikcetin/Eflatun.SceneReference.git#4.1.1";
        private static readonly string[] MackyIds =
        {
            "com.mackysoft.serializereference-extensions", // current ID in manifest
            "com.mackysoft.serializereferenceextensions"   // alt spelling
        };
        private const string MackyPrimaryId = "com.mackysoft.serializereference-extensions";
        private const string MackyUrl = "https://github.com/mackysoft/Unity-SerializeReferenceExtensions.git?path=Assets/MackySoft/MackySoft.SerializeReferenceExtensions";

        private const string DotweenDisplayName = "DOTween (HOTween v2)";
        private const string DotweenSettingsPath = "Assets/Resources/DOTweenSettings.asset";
        private const string LegacyDotweenPath = "Assets/Plugins/Demigiant/DOTween";

        private const string GameFrameworkRepo = "https://github.com/JorGra/JGameFramework.git";
        private const string GameFrameworkPath = "Assets/JGameFramework";

        private readonly StringBuilder _log = new StringBuilder();
        private Vector2 _scroll;
        private bool _busy;

        private StatusSnapshot _status;

        [MenuItem("Tools/JG/Project Setup", priority = 0)]
        private static void Open()
        {
            var wnd = GetWindow<JGProjectSetupWindow>("JG Project Setup");
            wnd.minSize = new Vector2(640, 520);
            wnd.RefreshStatus();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh status", GUILayout.Width(140)))
                    RefreshStatus();

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_busy))
                {
                    if (GUILayout.Button("Run All (A->D)", GUILayout.Width(150)))
                        RunAll();
                }
            }

            EditorGUILayout.Space(6);
            DrawStatusPanel();
            EditorGUILayout.Space(6);
            DrawActionsPanel();
            EditorGUILayout.Space(8);
            DrawLog();
        }

        private void DrawStatusPanel()
        {
            EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                StatusLine("Git repo", _status.IsGitRepo);
                StatusLine(".gitignore present", _status.HasGitignore);

                SubmoduleStatus("Essentials", _status.EssentialsState);
                StatusLine("DOTween (UPM/unitypackage)", _status.HasDotween);
                StatusLine("DOTween asmdefs enabled", _status.DotweenAsmdefsEnabled);
                StatusLine("UPM: Eflatun.SceneReference", _status.HasEflatun);
                StatusLine("UPM: MackySoft SerializeReferenceExtensions", _status.HasMacky);
                SubmoduleStatus("GameFramework", _status.GameFrameworkState);
            }
        }

        private void DrawActionsPanel()
        {
            EditorGUILayout.LabelField("Actions (run in order)", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(_busy))
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var stepAComplete = _status.IsGitRepo && _status.HasGitignore;
                var stepBComplete = _status.EssentialsState == SubmoduleState.SubmoduleOk;
                var stepCComplete = _status.HasDotween;

                using (new EditorGUI.DisabledScope(false))
                {
                    if (GUILayout.Button("Step A - Init Git + .gitignore"))
                        StepA();
                }

                using (new EditorGUI.DisabledScope(!stepAComplete))
                {
                    if (GUILayout.Button("Step B - Install/Convert Essentials submodule"))
                        StepB();
                }

                using (new EditorGUI.DisabledScope(!(stepAComplete && stepBComplete)))
                {
                    if (GUILayout.Button("Step C - Import & configure DOTween (.unitypackage)"))
                        StepC();
                }

                using (new EditorGUI.DisabledScope(!(stepAComplete && stepBComplete && stepCComplete)))
                {
                    if (GUILayout.Button("Step D - Install/Convert GameFramework submodule + UPM deps"))
                        StepD();
                }
            }
        }

        private void DrawLog()
        {
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(180));
                EditorGUILayout.TextArea(_log.ToString(), GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        #region Steps

        private void RunAll()
        {
            StepA();
            StepB();
            StepC(); // DOTween must be in place before GameFramework
            StepD();
            RefreshStatus();
        }

        private void StepA()
        {
            if (_busy) return;
            try
            {
                _busy = true;
                Append("Step A: Ensure Git + .gitignore");

                if (!EnsureGitRepo())
                    return;

                EnsureGitignore();
            }
            finally
            {
                _busy = false;
                RefreshStatus();
            }
        }

        private void StepB()
        {
            if (_busy) return;
            try
            {
                _busy = true;
                Append("Step B: Essentials submodule");

                if (!EnsureGitRepo())
                    return;

                EnsureSubmodule(EssentialsRepo, EssentialsPath, "JG-UnityEssentials");
            }
            finally
            {
                _busy = false;
                RefreshStatus();
            }
        }

        private void StepC()
        {
            if (_busy) return;
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("DOTween import", "Exit Play Mode before importing packages.", "OK");
                return;
            }
            try
            {
                _busy = true;
                Append("Step C: DOTween import from Asset Store cache or manual .unitypackage");
                ImportDotweenUnitypackage();
            }
            finally
            {
                _busy = false;
                RefreshStatus();
            }
        }

        private void StepD()
        {
            if (_busy) return;
            try
            {
                _busy = true;
                Append("Step D: GameFramework submodule + UPM deps");

                if (!EnsureGitRepo())
                    return;

                EnsureSubmodule(GameFrameworkRepo, GameFrameworkPath, "JGameFramework");
                EnsureUpmDependencies();
            }
            finally
            {
                _busy = false;
                RefreshStatus();
            }
        }

        #endregion

        #region Status detection

        private void RefreshStatus()
        {
            _status = new StatusSnapshot
            {
                IsGitRepo = Directory.Exists(Path.Combine(ProjectRoot, ".git")),
                HasGitignore = File.Exists(Path.Combine(ProjectRoot, ".gitignore")),
                EssentialsState = GetSubmoduleState(EssentialsPath),
                GameFrameworkState = GetSubmoduleState(GameFrameworkPath),
                HasEflatun = ManifestHasDependency(EflatunId, EflatunUrl),
                HasMacky = ManifestHasMacky(),
                HasDotween = ManifestHasAnyDotween() || LegacyDotweenExists(),
                DotweenAsmdefsEnabled = DotweenAsmdefsOn()
            };

            Repaint();
        }

        private SubmoduleState GetSubmoduleState(string targetPath)
        {
            var absPath = Path.Combine(ProjectRoot, targetPath);
            var exists = Directory.Exists(absPath);
            var isSubmodule = IsPathRegisteredSubmodule(targetPath);
            if (isSubmodule) return SubmoduleState.SubmoduleOk;
            if (exists) return SubmoduleState.PresentNotSubmodule;
            return SubmoduleState.Missing;
        }

        private void StatusLine(string label, bool ok, bool invert = false)
        {
            var state = invert ? !ok : ok;
            var color = state ? "green" : "red";
            GUILayout.Label(string.Format("<color={0}>- {1}: {2}</color>", color, label, state ? "OK" : "Missing"), new GUIStyle(EditorStyles.label) { richText = true });
        }

        private void SubmoduleStatus(string label, SubmoduleState state)
        {
            string text;
            string color;
            switch (state)
            {
                case SubmoduleState.SubmoduleOk:
                    text = "Submodule OK";
                    color = "green";
                    break;
                case SubmoduleState.PresentNotSubmodule:
                    text = "Present (not submodule)";
                    color = "yellow";
                    break;
                default:
                    text = "Missing";
                    color = "red";
                    break;
            }

            GUILayout.Label(string.Format("<color={0}>- {1}: {2}</color>", color, label, text),
                new GUIStyle(EditorStyles.label) { richText = true });
        }

        #endregion

        #region Git + submodules

        private bool EnsureGitRepo()
        {
            if (Directory.Exists(Path.Combine(ProjectRoot, ".git")))
            {
                Append("Git repo already initialized.");
                return true;
            }

            Append("Initializing git repository...");
            if (!RunGit("init", out var output))
            {
                AppendError("git init failed. Ensure git is installed and on PATH.");
                return false;
            }

            Append(output);
            return true;
        }

        private void EnsureGitignore()
        {
            var path = Path.Combine(ProjectRoot, ".gitignore");
            if (File.Exists(path))
            {
                Append(".gitignore already present.");
                return;
            }

            const string unityGitignore = @"# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
MemoryCaptures/
*.booproj
*.pidb
*.suo
*.user
*.userprefs
*.unityproj
*.dll
*.exe
*.pdf
*.mid
*.midi
*.wav
*.gif
*.ico
*.jpg
*.jpeg
*.png
*.psd
*.tga
*.tif
*.tiff
*.3ds
*.fbx
*.maya*
*.blend
*.mp3
*.ogg
*.aiff
*.aif
*.mod
*.it
*.s3m
*.xm
*.mov
*.mp4
*.mpg
*.mpeg
*.m4v
*.anim
*.unitypackage

# Auto-generated .csproj and solution
*.csproj
*.sln
*.slnx
*.unityproj

# Rider / VSCode
.idea/
.vs/
*.code-workspace
";

            File.WriteAllText(path, unityGitignore);
            Append("Created Unity .gitignore");
        }

        private void EnsureSubmodule(string repoUrl, string targetPath, string label)
        {
            var absPath = Path.Combine(ProjectRoot, targetPath);
            var state = GetSubmoduleState(targetPath);

            if (state == SubmoduleState.SubmoduleOk)
            {
                Append($"{label}: already a submodule. Syncing...");
                RunGit($"submodule sync -- \"{targetPath.Replace("\\", "/")}\"", out _);
                RunGit($"submodule update --init --recursive \"{targetPath.Replace("\\", "/")}\"", out _);
                return;
            }

            if (state == SubmoduleState.Missing)
            {
                Append($"{label}: adding submodule...");
                if (!RunGit($"submodule add {repoUrl} \"{targetPath.Replace("\\", "/")}\"", out var addOut))
                {
                    AppendError($"Failed to add submodule {label}.");
                    return;
                }

                Append(addOut);
                RunGit($"submodule update --init --recursive \"{targetPath.Replace("\\", "/")}\"", out _);
                return;
            }

            // Present but not submodule: convert carefully.
            var decision = EditorUtility.DisplayDialogComplex(
                $"{label} folder exists",
                $"{targetPath} exists but is not a git submodule.\n\nConvert by backing it up, adding the submodule fresh, and leaving your backup untouched.",
                "Convert", "Cancel", "Skip (leave as-is)");

            if (decision != 0) // not Convert
            {
                Append($"{label}: conversion skipped.");
                return;
            }

            var backupDir = Path.Combine(ProjectRoot, "Library/Temp/BootstrapperBackup-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            Directory.CreateDirectory(Path.GetDirectoryName(backupDir) ?? "Library/Temp");
            Directory.Move(absPath, backupDir);
            Append($"Moved existing folder to backup: {Relative(backupDir)}");

            if (!RunGit($"submodule add {repoUrl} \"{targetPath.Replace("\\", "/")}\"", out var addOutput))
            {
                AppendError($"Failed to add submodule {label} after backup. Your backup remains at {Relative(backupDir)}.");
                return;
            }

            Append(addOutput);
            RunGit($"submodule update --init --recursive \"{targetPath.Replace("\\", "/")}\"", out _);
            Append($"{label}: submodule added. Backup retained at {Relative(backupDir)} (manual merge if needed).");
        }

        private bool IsPathRegisteredSubmodule(string targetPath)
        {
            var gitmodulesPath = Path.Combine(ProjectRoot, ".gitmodules");
            if (!File.Exists(gitmodulesPath))
                return false;

            var text = File.ReadAllText(gitmodulesPath);
            var normalized = targetPath.Replace("\\", "/");
            return text.Contains("path = " + normalized);
        }

        #endregion

        #region Manifest / UPM

        private void EnsureUpmDependencies()
        {
            var manifest = LoadManifest();
            if (manifest == null)
            {
                AppendError("manifest.json not found.");
                return;
            }

            var changed = false;
            changed |= EnsureDependency(manifest, EflatunId, EflatunUrl);
            changed |= EnsureDependency(manifest, MackyPrimaryId, MackyUrl, MackyIds);

            if (changed)
            {
                SaveManifest(manifest);
                Append("Updated Packages/manifest.json with required dependencies.");
                TryResolvePackages();
            }
            else
            {
                Append("UPM deps already present.");
            }
        }

        private void ImportDotweenUnitypackage()
        {
            if (HasDotweenAlready())
            {
                Append("DOTween already present; import skipped.");
                TryEnableDotweenAsmdefs();
                return;
            }

            var cached = FindLatestDotweenPackage();
            if (!string.IsNullOrEmpty(cached))
            {
                Append("Importing DOTween from cache: " + cached);
                AssetDatabase.ImportPackage(cached, false);
                EditorApplication.delayCall += TryEnableDotweenAsmdefs;
                return;
            }

            var picked = EditorUtility.OpenFilePanel("Locate DOTween .unitypackage", Application.dataPath, "unitypackage");
            if (!string.IsNullOrEmpty(picked))
            {
                Append("Importing DOTween from selected file: " + picked);
                AssetDatabase.ImportPackage(picked, false);
                EditorApplication.delayCall += TryEnableDotweenAsmdefs;
            }
            else
            {
                Append("DOTween import canceled; no package selected.");
            }
        }

        private static JObject LoadManifest()
        {
            var path = Path.Combine(ProjectRoot, "Packages/manifest.json");
            if (!File.Exists(path)) return null;
            var text = File.ReadAllText(path);
            return JObject.Parse(text);
        }

        private static void SaveManifest(JObject manifest)
        {
            var path = Path.Combine(ProjectRoot, "Packages/manifest.json");
            File.WriteAllText(path, manifest.ToString(Newtonsoft.Json.Formatting.Indented));
            AssetDatabase.Refresh();
        }

        private bool EnsureDependency(JObject manifest, string id, string url, IEnumerable<string> aliases = null)
        {
            var deps = manifest["dependencies"] as JObject;
            if (deps == null)
            {
                deps = new JObject();
                manifest["dependencies"] = deps;
            }

            // Remove other entries pointing to same URL to avoid duplication (including aliases).
            var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { id };
            if (aliases != null)
            {
                foreach (var a in aliases) allNames.Add(a);
            }

            var keysToRemove = deps.Properties()
                .Where(p => !allNames.Contains(p.Name) && string.Equals((string)p.Value, url, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name)
                .ToList();
            foreach (var k in keysToRemove)
                deps.Remove(k);

            // If an alias exists with correct URL, normalize to primary id.
            if (aliases != null)
            {
                foreach (var alias in aliases)
                {
                    if (!string.Equals(alias, id, StringComparison.OrdinalIgnoreCase) &&
                        deps.Property(alias) != null &&
                        string.Equals((string)deps[alias], url, StringComparison.OrdinalIgnoreCase))
                    {
                        deps.Remove(alias);
                    }
                }
            }

            var current = deps[id];
            if (current != null && string.Equals((string)current, url, StringComparison.OrdinalIgnoreCase))
                return false;

            deps[id] = url;
            return true;
        }

        private bool ManifestHasDependency(string id, string url)
        {
            var path = Path.Combine(ProjectRoot, "Packages/manifest.json");
            if (!File.Exists(path)) return false;
            var manifest = JObject.Parse(File.ReadAllText(path));
            var deps = manifest["dependencies"] as JObject;
            if (deps == null) return false;
            var value = deps[id];
            return value != null && string.Equals((string)value, url, StringComparison.OrdinalIgnoreCase);
        }

        private bool ManifestHasAnyDotween()
        {
            var path = Path.Combine(ProjectRoot, "Packages/manifest.json");
            if (!File.Exists(path)) return false;
            var manifest = JObject.Parse(File.ReadAllText(path));
            var deps = manifest["dependencies"] as JObject;
            if (deps == null) return false;
            return deps.Properties().Any(p =>
            {
                var nameHas = p.Name.IndexOf("dotween", StringComparison.OrdinalIgnoreCase) >= 0;
                var val = (string)p.Value ?? string.Empty;
                var valHas = val.IndexOf("dotween", StringComparison.OrdinalIgnoreCase) >= 0;
                return nameHas || valHas;
            });
        }

        private bool ManifestHasMacky()
        {
            var path = Path.Combine(ProjectRoot, "Packages/manifest.json");
            if (!File.Exists(path)) return false;
            var manifest = JObject.Parse(File.ReadAllText(path));
            var deps = manifest["dependencies"] as JObject;
            if (deps == null) return false;

            foreach (var id in MackyIds)
            {
                var token = deps[id];
                if (token != null)
                {
                    var val = (string)token ?? string.Empty;
                    if (val.IndexOf("SerializeReferenceExtensions", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("mackysoft", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            // fallback: search by value only
            return deps.Properties().Any(p =>
            {
                var val = (string)p.Value ?? string.Empty;
                return val.IndexOf("SerializeReferenceExtensions", StringComparison.OrdinalIgnoreCase) >= 0 &&
                       val.IndexOf("mackysoft", StringComparison.OrdinalIgnoreCase) >= 0;
            });
        }

        private bool LegacyDotweenExists()
        {
            return Directory.Exists(Path.Combine(ProjectRoot, LegacyDotweenPath));
        }

        private bool HasDotweenAlready() => ManifestHasAnyDotween() || LegacyDotweenExists();

        private string FindLatestDotweenPackage()
        {
            var candidates = new List<string>();

            // Windows Asset Store cache
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrEmpty(appData))
            {
                var winCache = Path.Combine(appData, "Unity", "Asset Store-5.x");
                candidates.AddRange(FindPackagesInRoot(winCache));
            }

            // macOS Asset Store cache
            var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (!string.IsNullOrEmpty(home))
            {
                var macCache = Path.Combine(home, "Library", "Unity", "Asset Store-5.x");
                candidates.AddRange(FindPackagesInRoot(macCache));
            }

            // Choose newest by write time
            var newest = candidates
                .Select(path => new { path, time = File.GetLastWriteTimeUtc(path) })
                .OrderByDescending(x => x.time)
                .FirstOrDefault();

            return newest?.path;
        }

        private static IEnumerable<string> FindPackagesInRoot(string root)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return Enumerable.Empty<string>();

            try
            {
                return Directory.GetFiles(root, "*dotween*.unitypackage", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        }

        private void TryResolvePackages()
        {
            try
            {
                Client.Resolve();
                Append("Package resolve requested.");
            }
            catch (Exception ex)
            {
                Append($"Package resolve not available in this Unity version: {ex.Message}");
            }
        }

        #endregion

        #region DOTween settings

        private void TryEnableDotweenAsmdefs()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(DotweenSettingsPath);
            if (settings == null)
            {
                // Try find by search if moved.
                var guids = AssetDatabase.FindAssets("DOTweenSettings t:ScriptableObject");
                if (guids.Length > 0)
                    settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (settings == null)
            {
                Append("DOTweenSettings asset not found; asmdef toggle skipped.");
                return;
            }

            var field = settings.GetType().GetField("createASMDEF",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
            {
                var current = (bool)field.GetValue(settings);
                if (!current)
                {
                    field.SetValue(settings, true);
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Append("Enabled createASMDEF in DOTweenSettings.");
                }
                else
                {
                    Append("DOTweenSettings already has asmdefs enabled.");
                }
            }
            else
            {
                Append("DOTweenSettings does not expose createASMDEF; no change made.");
            }
        }

        private bool DotweenAsmdefsOn()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(DotweenSettingsPath);
            if (settings == null) return false;
            var field = settings.GetType().GetField("createASMDEF",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(settings);
            return false;
        }

        // No Package Manager callback needed for unitypackage import.

        #endregion

        #region Git helper

        private bool RunGit(string arguments, out string output)
        {
            output = string.Empty;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = ProjectRoot,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc == null)
                    {
                        AppendError("Failed to start git process.");
                        return false;
                    }

                    var stdout = proc.StandardOutput.ReadToEnd();
                    var stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    output = stdout + stderr;
                    var success = proc.ExitCode == 0;
                    if (!success)
                        AppendError("git " + arguments + " failed: " + output);
                    else
                        Append("git " + arguments + " ok.");

                    return success;
                }
            }
            catch (Exception ex)
            {
                AppendError("git error: " + ex.Message);
                return false;
            }
        }

        #endregion

        #region Logging helpers

        private void Append(string msg)
        {
            _log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
            Repaint();
        }

        private void AppendError(string msg)
        {
            _log.AppendLine($"[{DateTime.Now:HH:mm:ss}] ERROR: {msg}");
            Repaint();
        }

        private static string Relative(string absPath)
        {
            var root = ProjectRoot.Replace("\\", "/");
            absPath = absPath.Replace("\\", "/");
            if (absPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return absPath.Substring(root.Length).TrimStart('/');
            return absPath;
        }

        #endregion

        #region Data

        private static string ProjectRoot
        {
            get
            {
                var assets = Application.dataPath.Replace("\\", "/");
                return assets.Substring(0, assets.Length - "/Assets".Length);
            }
        }

        private struct StatusSnapshot
        {
            public bool IsGitRepo;
            public bool HasGitignore;
            public SubmoduleState EssentialsState;
            public SubmoduleState GameFrameworkState;
            public bool HasEflatun;
            public bool HasMacky;
            public bool HasDotween;
            public bool DotweenAsmdefsEnabled;
        }

        private enum SubmoduleState
        {
            Missing,
            PresentNotSubmodule,
            SubmoduleOk
        }

        #endregion
    }
}
