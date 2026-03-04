using MonoDetour;
using MonoDetour.HookGen;

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
			ItemPlugin.Log.LogError($"Attempted to update to mod equipment id {((int)itemId).ToString()}");
		}
	}
}