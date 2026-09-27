using System;
using System.Collections.Generic;
using HarmonyLib;

namespace NotMyInventory.Patches;

/// <summary>
/// The game already has an anti-theft enchantment (ENC.negateSteal, 426). Rogues skimming gold when you
/// swap places (Chara.TryPush) and pickpocketing (AI_Steal) both check it via HasElement, so the player
/// is reported as always having it.
/// </summary>
[HarmonyPatch(typeof(Card), nameof(Card.HasElement), typeof(int), typeof(bool))]
internal static class NegateStealPatch
{
    private static void Postfix(Card __instance, int ele, ref bool __result)
    {
        if (ele != ENC.negateSteal || __result || !Plugin.PreventTheft.Value || !Protection.IsProtectedOwner(__instance))
        {
            return;
        }

        __result = true;

        // TryPush only checks negateSteal once a rogue has actually rolled to skim your gold.
        if (SwapPlacesPatch.Point is { } point)
        {
            var rogue = point.ListCharas().Find(c => c.trait is TraitRogue && !c.IsPC);
            Notify.Theft(rogue, "pick your pocket");
        }
    }
}

[HarmonyPatch(typeof(Chara), nameof(Chara.TryPush))]
internal static class SwapPlacesPatch
{
    internal static Point? Point;

    private static void Prefix(Chara __instance, Point point)
    {
        Point = __instance.IsPC ? point : null;
    }

    private static void Finalizer()
    {
        Point = null;
    }
}

/// <summary>
/// AI_Steal lets thieves with the "master thief" feat (1662) bypass negateSteal, so cancel it outright
/// whenever someone other than the player targets the player or anything they carry.
/// </summary>
[HarmonyPatch(typeof(AI_Steal), nameof(AI_Steal.Run))]
internal static class PickpocketPatch
{
    private static bool Prefix(AI_Steal __instance, ref IEnumerable<AIAct.Status> __result)
    {
        if (!Plugin.PreventTheft.Value || Protection.IsProtectedOwner(__instance.owner))
        {
            return true;
        }

        var target = __instance.target;
        if (!Protection.IsProtectedOwner(target) && !Protection.IsInProtectedInventory(target))
        {
            return true;
        }

        Notify.Theft(__instance.owner, "pick your pocket");
        __result = Cancelled(__instance);
        return false;
    }

    private static IEnumerable<AIAct.Status> Cancelled(AI_Steal ai)
    {
        yield return ai.Cancel();
    }
}

/// <summary>
/// Road encounters where a mob demands one of your items: agreeing still makes peace (and gives karma),
/// but the item never leaves your inventory.
/// </summary>
[HarmonyPatch(typeof(ZonePreEnterEncounter), nameof(ZonePreEnterEncounter.Execute))]
internal static class EncounterTollPatch
{
    internal static bool Active;

    private static void Prefix(out Action? __state)
    {
        __state = LayerDrama.refAction2;
    }

    private static void Postfix(Action? __state)
    {
        var handOver = LayerDrama.refAction2;
        if (handOver is null || handOver == __state)
        {
            return;
        }

        LayerDrama.refAction2 = () =>
        {
            Active = Plugin.PreventTheft.Value;
            try
            {
                handOver();
            }
            finally
            {
                Active = false;
            }
        };
    }
}

[HarmonyPatch(typeof(Card), nameof(Card.AddCard))]
internal static class EncounterTollAddCardPatch
{
    private static bool Prefix(Card __instance, Card c, ref Card __result)
    {
        if (!EncounterTollPatch.Active || !Protection.IsInProtectedInventory(c) || Protection.IsProtectedOwner(__instance))
        {
            return true;
        }

        Notify.Theft(__instance, $"take your {c.Name}");
        __result = c;
        return false;
    }
}
