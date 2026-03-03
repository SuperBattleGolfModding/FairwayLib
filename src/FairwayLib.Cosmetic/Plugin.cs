using BepInEx;
using BepInEx.Logging;

namespace FairwayLib.Cosmetic
{
    [BepInAutoPlugin]
    public partial class CosmeticPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin {Name} is loaded!");
        }
    }
}
