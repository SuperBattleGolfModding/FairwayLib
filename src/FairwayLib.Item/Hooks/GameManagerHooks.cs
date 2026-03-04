using MonoDetour;
using MonoDetour.HookGen;

namespace FairwayLib.Item.Hooks;

[MonoDetourTargets(typeof(GameManager))]
public class GameManagerHooks
{
    [MonoDetourHookInitialize]
    static void Init()
    {
        Md.GameManager.Awake.Postfix(Postfix_Awake);
    }

    static void Postfix_Awake(GameManager self)
    {
        /* TODO:
         * Modded items should be injected here
         */
    }

     static void _injectModdedItems(int modIndex, ItemCollection collection)
    {
        try
        {
            if (GameManager.instance?.allItems?.allItemData == null)
            {
                return;
            }
            
            if (collection.items == null)
            {
                ItemPlugin.Log.LogError("modCollection.items is NULL!");
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
                    ItemPlugin.Log.LogInfo($"Added modded item {itemPrefabName} with id {newItemType}");
                    itemIndex++;
                }
                else
                {
                    ItemPlugin.Log.LogError($"Failed to add item {itemPrefabName} with id {newItemType}");
                }
            }
        }
        catch (System.Exception e)
        {
            ItemPlugin.Log.LogError($"CRASH en el loop: {e}");
        }
    }
}