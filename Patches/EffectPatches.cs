using HarmonyLib;

namespace NotMyInventory.Patches;

/// <summary>
/// Every Proc overload funnels into this one. Blocks effects aimed at the player that take or ruin items:
/// Steal (items, food and gold; used by gnomes, thieves and anything else with a steal ability),
/// Acid (corrodes equipment) and CurseEQ (curses/dooms equipment).
/// </summary>
[HarmonyPatch(typeof(ActEffect), nameof(ActEffect.Proc),
    typeof(EffectId), typeof(int), typeof(BlessedState), typeof(Card), typeof(Card), typeof(ActRef))]
internal static class HostileEffectPatch
{
    private static bool Prefix(EffectId id, Card cc, Card? tc, ActRef actRef)
    {
        var target = tc ?? cc;
        if (!Protection.IsProtectedOwner(target))
        {
            return true;
        }

        switch (id)
        {
            case EffectId.Steal when Plugin.PreventTheft.Value:
                var attempt = actRef.n1 switch
                {
                    "money" => "steal your gold",
                    "food" => "steal your food",
                    _ => "steal from you",
                };
                Notify.Theft(cc == target ? null : cc, attempt);
                return false;
            case EffectId.Acid when Plugin.PreventAcid.Value:
                Notify.Damage("Acid", "equipment corrosion");
                return false;
            case EffectId.CurseEQ when Plugin.PreventCurse.Value:
                Notify.Damage("Curse", "equipment curse");
                return false;
            default:
                return true;
        }
    }
}
