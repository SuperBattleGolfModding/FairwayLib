using HarmonyLib;
using MonoDetour;
using MonoDetour.HookGen;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace FairwayLib.Item.Hooks;

[MonoDetourTargets(typeof(PlayerInventory))]
public class PlayerInventoryHooks
{
	[MonoDetourHookInitialize]
	static void Init()
	{
		Md.PlayerInventory.UpdateEquipmentSwitchers.Postfix(Postfix_UpdateEquipmentSwitchers);
	}

	static void Postfix_UpdateEquipmentSwitchers(PlayerInventory self)
	{
		var itemId = (EquipmentType)self.GetEffectivelyEquippedItem();
		if ((int)itemId >= 1000)
		{
			self.PlayerInfo.RightHandEquipmentSwitcher.SetEquipment(self.thrownItem.HasFlag(PlayerInventory.ThrownItemHand.Right) ? EquipmentType.None : itemId);
			ItemPlugin.Log.LogError($"Attempted to update to mod equipment id {(int)itemId}");
		}
	}
}

[HarmonyPatch]
public static class GetItemUseRoutine_Patch
{
	[HarmonyTargetMethod]
	static MethodBase TargetMethod()
	{
        Type displayClass = AccessTools.Inner(typeof(PlayerInventory), "<>c__DisplayClass117_0");
        return AccessTools.Method(displayClass, "<TryUseItem>g__GetItemUseRoutine|0");
    }

    [HarmonyPrefix]
    static bool Prefix(object __instance, ref IEnumerator __result)
    {
        var inventoryField = AccessTools.Field(__instance.GetType(), "<>4__this");
        PlayerInventory inventory = (PlayerInventory)inventoryField.GetValue(__instance);

        var equippedSlotField = AccessTools.Field(__instance.GetType(), "equippedSlot");
        var equippedSlot = equippedSlotField.GetValue(__instance);

        var itemTypeField = AccessTools.Field(equippedSlot.GetType(), "itemType");
        var itemType = (ItemType)itemTypeField.GetValue(equippedSlot);

        if ((int)itemType >= 1000)
		{
			__result = CustomItemRoutine(inventory);
			return false;
		}
		return true;
    }

	static IEnumerator CustomItemRoutine(PlayerInventory inventory)
	{
		ItemPlugin.Log.LogInfo("Activated test item for 3 seconds...");
		inventory.SetCurrentItemUse(ItemUseType.Regular);
		inventory.DecrementUseFromSlotAt(inventory.EquippedItemIndex);
		yield return new WaitForSeconds(3);
		ItemPlugin.Log.LogInfo("Yipee!");
        inventory.SetCurrentItemUse(ItemUseType.None);
        inventory.RemoveIfOutOfUses(inventory.EquippedItemIndex);
    }
}