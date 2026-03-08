using BepInEx;
using BepInEx.Logging;
using FairwayLib.Core;
using HarmonyLib;
using MonoDetour;
using System.IO;
using UnityEngine;

namespace FairwayLib.Item
{
    [BepInAutoPlugin]
    [BepInDependency(CorePlugin.Id)]
    public partial class ItemPlugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new(Id);
        internal static ManualLogSource Log { get; private set; } = null!;
        public static AssetBundle itemBundle = AssetBundle.LoadFromFile(Path.Combine(Paths.PluginPath, "FairwayLib", "items"));
        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin {Name} is loaded!");
            MonoDetourManager.InvokeHookInitializers(typeof(ItemPlugin).Assembly);
            harmony.PatchAll();
        }
    }
}
