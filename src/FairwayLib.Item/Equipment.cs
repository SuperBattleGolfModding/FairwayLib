using HarmonyLib;

namespace FairwayLib.Item;

[HarmonyPatch(typeof(CourseManager), "Awake")]
public class CourseManager_Patch
{
    [HarmonyPostfix]
    static void Postfix(EquipmentCollection __instance)
    {
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



[HarmonyPatch(typeof(PlayerInventory), "UpdateEquipmentSwitchers")]
public class PlayerInventory_UpdateEquipmentSwitchers
{
    [HarmonyPostfix]
    public static void Postfix(PlayerInventory __instance)
    {
        var itemId = (EquipmentType)__instance.GetEffectivelyEquippedItem();
        if ((int)itemId >= 1000)
        {
            __instance.PlayerInfo.RightHandEquipmentSwitcher.SetEquipment(__instance.thrownItem.HasFlag(PlayerInventory.ThrownItemHand.Right) ? EquipmentType.None : itemId);   
            ItemPlugin.Log.LogError($"Attempted to update to mod equipment id {((int)itemId).ToString()}");
        }
    }
}