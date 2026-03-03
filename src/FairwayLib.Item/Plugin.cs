using BepInEx;
using BepInEx.Logging;

namespace FairwayLib.Item
{
    [BepInAutoPlugin]
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
