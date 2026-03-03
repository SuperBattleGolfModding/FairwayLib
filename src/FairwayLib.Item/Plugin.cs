using BepInEx;
using BepInEx.Logging;
using FairwayLib.Core;

namespace FairwayLib.Item
{
    [BepInAutoPlugin]
    [BepInDependency(CorePlugin.Id)]
    public partial class ItemPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin {Name} is loaded!");
        }
    }
}
