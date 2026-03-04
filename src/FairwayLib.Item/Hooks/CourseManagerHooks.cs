using MonoDetour;
using MonoDetour.HookGen;

namespace FairwayLib.Item.Hooks;

[MonoDetourTargets(typeof(CourseManager))]
public class CourseManagerHooks
{
    [MonoDetourHookInitialize]
    static void Init()
    {
        Md.CourseManager.Awake.Postfix(Postfix_Awake);
    }

    static void Postfix_Awake(CourseManager self)
    {
        /*
         * TODO:
         * Mods' modded equipments are supposed to be injected here
         */
    }

    private static void _injectModdedEquipments(int modIndex, EquipmentCollection collection)
    {
        try
        {
            if (EquipmentManager.instance?.equipmentCollection?.equipmentDictionary == null)
            {
                ItemPlugin.Log.LogError("EquipmentManager dictionary is not ready yet!");
                return;
            }
            
            if (collection.equipment == null)
            {
                ItemPlugin.Log.LogError("equipmentCollection.items is NULL!");
                return;
            }

            var gameEquipmentData = EquipmentManager.instance.equipmentCollection.equipmentDictionary;
            ItemPlugin.Log.LogInfo($"Starting loop for {collection.equipment.Length} items...");
            var equipmentIndex = 0;
            foreach (var equipment in collection.equipment)
            {
                if (equipment == null) continue;
                var newEquipmentType = (EquipmentType)(modIndex * 1000 + equipmentIndex);
                equipment.Type = newEquipmentType;
                var equipmentPrefabName = equipment.Prefab.name;
                if (gameEquipmentData.TryAdd(newEquipmentType, equipment))
                {
                    ItemPlugin.Log.LogInfo($"Added modded equipment {equipmentPrefabName} with id {newEquipmentType}");
                    equipmentIndex++;
                }
                else
                {
                    ItemPlugin.Log.LogError($"Failed to add equipment {equipmentPrefabName} with id {newEquipmentType}");
                }
            }
        }
        catch (System.Exception e)
        {
            ItemPlugin.Log.LogError($"CRASH en el loop: {e}");
        }
    }
}