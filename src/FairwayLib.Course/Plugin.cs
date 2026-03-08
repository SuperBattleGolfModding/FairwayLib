using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Eflatun.SceneReference;
using FairwayLib.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FairwayLib.Course
{
    /// <summary>
    /// Public API surface for FairwayLib.CustomCourse.
    /// Call RegisterNewMap() from any BepInEx plugin Awake() to inject a custom map.
    /// </summary>
    public static class CustomCourseAPI
    {
        internal static readonly List<PendingMapEntry> PendingMaps = new List<PendingMapEntry>();

        /// <summary>
        /// Registers a custom map to be injected into the course list at runtime.
        /// Call this from your plugin's Awake() — before the game's CourseCollection initialises.
        /// </summary>
        /// <param name="assetBundlePath">Absolute or relative path to the data AssetBundle (contains CourseData / HoleData assets).</param>
        /// <param name="sceneBundlePath">Absolute or relative path to the scene AssetBundle.</param>
        public static void RegisterNewMap(string assetBundlePath, string sceneBundlePath)
        {
            if (string.IsNullOrWhiteSpace(assetBundlePath))
                throw new ArgumentException("assetBundlePath must not be null or empty.", nameof(assetBundlePath));
            if (string.IsNullOrWhiteSpace(sceneBundlePath))
                throw new ArgumentException("sceneBundlePath must not be null or empty.", nameof(sceneBundlePath));

            PendingMaps.Add(new PendingMapEntry
            {
                AssetBundlePath = assetBundlePath,
                SceneBundlePath = sceneBundlePath
            });

            CoursePlugin.Log.LogInfo($"[CustomCourseAPI] Queued map: {assetBundlePath}");
        }
    }

    [BepInAutoPlugin]
    [BepInDependency(CorePlugin.Id)]
    public partial class CoursePlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;
        private Harmony _harmony = new(Id);
        private EquipmentCollection baseEquipmentCollection;
        private PhysicsSettings basePhysicsSettings;
        private ThrownUsedItem[] baseThrownUsedItem;
        private PlayerCosmeticsVictoryDances basePlayerCosmeticsVictoryDances;
        private string orbitCameraModulejs;
        private static Harmony _harmonyInstance;

        private static readonly string CustomMapsRoot = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".",
            "..", "..", "CustomMaps");

        private void Awake()
        {
            Log = base.Logger;
            Log.LogInfo($"Plugin {Name} is loaded!");

            Directory.CreateDirectory(CustomMapsRoot);
            Log.LogInfo($"CustomMaps folder: {Path.GetFullPath(CustomMapsRoot)}");

            DiscoverMapsFromFolder();
            _harmony.PatchAll(typeof(CoursePlugin).Assembly);
            _harmonyInstance = _harmony;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void DiscoverMapsFromFolder()
        {
            if (!Directory.Exists(CustomMapsRoot))
                return;

            foreach (var mapDir in Directory.GetDirectories(CustomMapsRoot))
            {
                string assetPath = Path.Combine(mapDir, "MapAsset.assetbundle");
                string scenePath = Path.Combine(mapDir, "MapScene.assetbundle");

                if (!File.Exists(assetPath))
                {
                    Log.LogWarning($"[Discover] Skipping '{Path.GetFileName(mapDir)}': missing MapAsset.assetbundle");
                    continue;
                }
                if (!File.Exists(scenePath))
                {
                    Log.LogWarning($"[Discover] Skipping '{Path.GetFileName(mapDir)}': missing MapScene.assetbundle");
                    continue;
                }

                CustomCourseAPI.RegisterNewMap(assetPath, scenePath);
                Log.LogInfo($"[Discover] Found map: {Path.GetFileName(mapDir)}");
            }
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name.ToLower().Contains("range"))
            {
                StartCoroutine(BackupRoutine());
            }
            else if (arg0.name.ToLower().Contains("dust"))
            {
                StartCoroutine(RestoreRoutine());
            }
        }

        private IEnumerator BackupRoutine()
        {
            EquipmentManager equipmentManager = GameObject.FindAnyObjectByType<EquipmentManager>(FindObjectsInactive.Include);
            baseEquipmentCollection = equipmentManager.equipmentCollection;

            PhysicsManager physicsManager = GameObject.FindAnyObjectByType<PhysicsManager>(FindObjectsInactive.Include);
            basePhysicsSettings = physicsManager.settings;

            ThrownUsedItemManager thrownUsedItemManager = GameObject.FindAnyObjectByType<ThrownUsedItemManager>(FindObjectsInactive.Include);
            baseThrownUsedItem = thrownUsedItemManager.prefabs;

            CosmeticsUnlocksManager cosmeticsUnlocksManager = GameObject.FindAnyObjectByType<CosmeticsUnlocksManager>(FindObjectsInactive.Include);
            basePlayerCosmeticsVictoryDances = cosmeticsUnlocksManager.allDances;

            OrbitCameraModule orbitCameraModule = GameObject.FindAnyObjectByType<OrbitCameraModule>(FindObjectsInactive.Include);
            orbitCameraModulejs = JsonUtility.ToJson(orbitCameraModule);

            yield return null;
            Debug.Log("Backed up all relevant data");
        }

        private IEnumerator RestoreRoutine()
        {
            Debug.Log("Applying all backed up data");

            EquipmentManager equipmentManager = GameObject.FindAnyObjectByType<EquipmentManager>(FindObjectsInactive.Include);
            equipmentManager.equipmentCollection = baseEquipmentCollection;

            PhysicsManager physicsManager = GameObject.FindAnyObjectByType<PhysicsManager>(FindObjectsInactive.Include);
            physicsManager.settings = basePhysicsSettings;

            ThrownUsedItemManager thrownUsedItemManager = GameObject.FindAnyObjectByType<ThrownUsedItemManager>(FindObjectsInactive.Include);
            thrownUsedItemManager.prefabs = baseThrownUsedItem;

            CosmeticsUnlocksManager cosmeticsUnlocksManager = GameObject.FindAnyObjectByType<CosmeticsUnlocksManager>(FindObjectsInactive.Include);
            cosmeticsUnlocksManager.allDances = basePlayerCosmeticsVictoryDances;

            PlayerOcclusionManager playerOcclusionManager = GameObject.FindAnyObjectByType<PlayerOcclusionManager>(FindObjectsInactive.Include);
            playerOcclusionManager.Awake();

            OrbitCameraModule orbitCameraModule = GameObject.FindAnyObjectByType<OrbitCameraModule>(FindObjectsInactive.Include);
            JsonUtility.FromJsonOverwrite(orbitCameraModulejs, orbitCameraModule);

            yield return null;
        }

        [HarmonyPatch(typeof(CourseCollection), "RuntimeInitialize")]
        public static class Patch_CourseCollection_RuntimeInitialize
        {
            static void Prefix(CourseCollection __instance)
            {
                if (CustomCourseAPI.PendingMaps.Count == 0)
                {
                    Log.LogInfo("[Patch] No custom maps registered.");
                    return;
                }

                CourseData fallbackCourse = null;
                var fallbackHoles = new List<HoleData>();

                var standaloneCoursesToAdd = new List<CourseData>();

                foreach (var entry in CustomCourseAPI.PendingMaps)
                {
                    Log.LogInfo($"[Patch] Loading map: {entry.AssetBundlePath}");

                    AssetBundle dataBundle = null;
                    AssetBundle sceneBundle = null;

                    try
                    {
                        dataBundle = AssetBundle.LoadFromFile(entry.AssetBundlePath);
                        sceneBundle = AssetBundle.LoadFromFile(entry.SceneBundlePath);
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"[Patch] Failed to load bundles for '{entry.AssetBundlePath}': {ex.Message}");
                        dataBundle?.Unload(false);
                        sceneBundle?.Unload(false);
                        continue;
                    }

                    if (dataBundle == null || sceneBundle == null)
                    {
                        Log.LogError($"[Patch] Null bundle after load for '{entry.AssetBundlePath}'");
                        dataBundle?.Unload(false);
                        sceneBundle?.Unload(false);
                        continue;
                    }

                    CourseData bundleCourse = dataBundle.LoadAsset<CourseData>("Course settings");

                    if (bundleCourse != null)
                    {
                        Log.LogInfo($"[Patch] Bundle has its own CourseData: {bundleCourse.LocalizedName}");
                        RegisterHolesForCourse(bundleCourse, sceneBundle);
                        standaloneCoursesToAdd.Add(bundleCourse);
                    }
                    else
                    {
                        Log.LogInfo("[Patch] No CourseData in bundle — adding holes to 'Custom' course.");
                        var holes = dataBundle.LoadAllAssets<HoleData>();
                        if (holes == null || holes.Length == 0)
                        {
                            Log.LogWarning($"[Patch] No HoleData assets found in '{entry.AssetBundlePath}'");
                        }
                        else
                        {
                            foreach (var hole in holes)
                            {
                                RegisterHoleScene(hole, sceneBundle);
                                fallbackHoles.Add(hole);
                            }
                        }
                    }

                }

                if (fallbackHoles.Count > 0)
                {
                    fallbackCourse = CreateFallbackCourse(fallbackHoles);
                    standaloneCoursesToAdd.Insert(0, fallbackCourse);
                }

                if (standaloneCoursesToAdd.Count > 0)
                {
                    var newArray = new List<CourseData>(standaloneCoursesToAdd);
                    foreach (var existing in __instance.Courses)
                        newArray.Add(existing);
                    __instance.Courses = newArray.ToArray();

                    foreach (var c in standaloneCoursesToAdd)
                        Log.LogInfo($"[Patch] Injected course: {c.LocalizedName}");
                }
            }

            private static void RegisterHolesForCourse(CourseData course, AssetBundle sceneBundle)
            {
                foreach (var hole in course.Holes)
                    RegisterHoleScene(hole, sceneBundle);
            }

            private static void RegisterHoleScene(HoleData hole, AssetBundle sceneBundle)
            {
                if (hole == null || string.IsNullOrWhiteSpace(hole.name))
                {
                    Log.LogWarning("[Patch] Skipping hole with empty name.");
                    return;
                }

                var scenePath = sceneBundle.GetAllScenePaths()
                                           .FirstOrDefault(p => p.EndsWith(hole.name + ".unity"));

                if (scenePath == null)
                {
                    Log.LogWarning($"[Patch] Could not find scene for hole '{hole.name}' in scene bundle.");
                    foreach (var p in sceneBundle.GetAllScenePaths())
                        Log.LogWarning($"[Patch]   Available scene: {p}");
                    return;
                }

                string guid = Guid.NewGuid().ToString("N");
                RegisterScene(guid, scenePath);

                if (hole.Scene == null)
                    hole.Scene = new SceneReference();

                hole.Scene.FillWithDeserializedGuid(guid);

                Log.LogInfo($"[Patch] Hole '{hole.name}' → GUID {guid} → {scenePath}");
            }

            /// <summary>
            /// Creates a synthetic "Custom" CourseData to house any holes that didn't
            /// ship with their own CourseData.
            /// </summary>
            private static CourseData CreateFallbackCourse(List<HoleData> holes)
            {
                var course = ScriptableObject.CreateInstance<CourseData>();
                course.name = "Custom";

                // Give it a plain English localised name
                var localizedName = new UnityEngine.Localization.LocalizedString
                {
                    TableReference = "UI Text",
                    TableEntryReference = "Custom"
                };
                course.SetLocalizedName(localizedName);

                course.MenuBackgroundColor = new Color(0.15f, 0.15f, 0.15f);
                course.MenuForegroundColor = Color.white;
                course.OverrideHoles(holes.ToArray());

                Log.LogInfo($"[Patch] Created fallback 'Custom' course with {holes.Count} hole(s).");
                return course;
            }
        }

        static void RegisterScene(string guid, string path)
        {
            var type = typeof(SceneGuidToPathMapProvider);

            var guidToPathField = type.GetField("_sceneGuidToPathMap", BindingFlags.Static | BindingFlags.NonPublic);
            var pathToGuidField = type.GetField("_scenePathToGuidMap", BindingFlags.Static | BindingFlags.NonPublic);

            if (guidToPathField == null || pathToGuidField == null)
            {
                Log.LogError("Reflection failed: sceneGuidToPathMap or scenePathToGuidMap not found!");
                return;
            }

            var guidToPath = (Dictionary<string, string>)guidToPathField.GetValue(null);
            var pathToGuid = (Dictionary<string, string>)pathToGuidField.GetValue(null);

            if (guidToPath == null)
            {
                guidToPath = new Dictionary<string, string>();
                guidToPathField.SetValue(null, guidToPath);
            }

            if (pathToGuid == null)
            {
                pathToGuid = new Dictionary<string, string>();
                pathToGuidField.SetValue(null, pathToGuid);
            }

            guidToPath[guid] = path;
            pathToGuid[path] = guid;
        }
    }
    public class PendingMapEntry
    {
        public string AssetBundlePath;
        public string SceneBundlePath;
    }
}
