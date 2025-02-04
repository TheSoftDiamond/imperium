#region

using HarmonyLib;
using UnityEngine;

#endregion

namespace Imperium.Patches.Objects;

[HarmonyPatch(typeof(ItemDropship))]
public static class ItemDropshipPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("DeliverVehicleClientRpc")]
    internal static void DeliverVehicleClientRpcPostfixPatch(ItemDropship __instance)
    {
        Imperium.ObjectManager.RefreshLevelObjects();
    }
}