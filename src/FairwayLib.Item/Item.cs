using HarmonyLib;

namespace FairwayLib.Item;

[HarmonyPatch(typeof(GameManager), "Awake")]
public class GameManager_Patch
{
    [HarmonyPostfix]
    public static void Postfix(GameManager __instance)
    {
    }
    
    private static void _injectModdedItems(int modIndex, ItemCollection collection)
    {
        try
        {
            if (GameManager.instance?.allItems?.allItemData == null)
            {
                return;
            }
            
            if (collection.items == null)
            {
                ItemPlugin.Logger.LogError("modCollection.items is NULL!");
                return;
            }
            var gameItemData = GameManager.instance.allItems.allItemData;
            var itemIndex = 0;
            foreach (var item in collection.items)
            {
                if (item == null) continue;
                var newItemType = (ItemType)(modIndex * 1000 + itemIndex);
                item.Type = newItemType;
                var itemPrefabName = item.Prefab.name;
                item.Initialize();
                if (gameItemData.TryAdd(newItemType, item))
                {
                    ItemPlugin.Logger.LogInfo($"Added modded item {itemPrefabName} with id {newItemType}");
                    itemIndex++;
                }
                else
                {
                    ItemPlugin.Logger.LogError($"Failed to add item {itemPrefabName} with id {newItemType}");
                }
            }
        }
        catch (System.Exception e)
        {
            ItemPlugin.Logger.LogError($"CRASH en el loop: {e}");
        }
    }
}