using System.Collections.Generic;
using HarmonyLib;

namespace NotMyInventory.Patches;

/// <summary>
/// Map.TryShatter is where fire and cold burn, shatter or cook items carried by characters on a tile,
/// and where fireproof/coldproof blankets lose charges. Its first call to Point.ListCards gathers the
/// cards on the tile, so the player is dropped from that list and nothing they carry is touched.
/// </summary>
[HarmonyPatch(typeof(Map), nameof(Map.TryShatter))]
internal static class ShatterPatch
{
    internal static bool FilterNextListCards;
    internal static Point? Pos;
    internal static int Ele;

    private static void Prefix(Point pos, int ele)
    {
        FilterNextListCards = Plugin.PreventElementalDestruction.Value;
        Pos = pos;
        Ele = ele;
    }

    private static void Finalizer()
    {
        FilterNextListCards = false;
        Pos = null;
    }

    /// <summary>Mirrors the game's early-outs so the log only reports hits that could have damaged something.</summary>
    internal static bool WouldHaveHitPlayer(Chara pc)
    {
        var fire = Ele == 910;
        if (fire && Pos is { } pos && (pos.cell.IsSnowTile || pos.cell.IsTopWater))
        {
            return false;
        }

        return pc.ResistLvFrom(Ele) < 3 && pc.things.Count > 0;
    }
}

[HarmonyPatch(typeof(Point), nameof(Point.ListCards))]
internal static class ShatterListCardsPatch
{
    private static void Postfix(List<Card> __result)
    {
        if (!ShatterPatch.FilterNextListCards)
        {
            return;
        }

        ShatterPatch.FilterNextListCards = false;
        var pc = EClass.pc;
        if (pc is null || !__result.Remove(pc))
        {
            return;
        }

        if (ShatterPatch.WouldHaveHitPlayer(pc))
        {
            Notify.Damage(EClass.sources.elements.map[ShatterPatch.Ele].GetName().ToTitleCase());
        }
    }
}

/// <summary>Safety net: nothing in the player's inventory can take HP damage (and so can't be destroyed by it).</summary>
[HarmonyPatch(typeof(Card), nameof(Card.DamageHP),
    typeof(long), typeof(int), typeof(int), typeof(AttackSource), typeof(Card), typeof(bool), typeof(Thing), typeof(Chara), typeof(int))]
internal static class CarriedItemDamagePatch
{
    private static bool Prefix(Card __instance, int ele)
    {
        if (!Plugin.PreventElementalDestruction.Value || !Protection.IsInProtectedInventory(__instance))
        {
            return true;
        }

        var source = EClass.sources.elements.map.TryGetValue(ele) is { } row && ele != 0 ? row.GetName().ToTitleCase() : __instance.Name;
        Notify.Damage(source);
        return false;
    }
}
