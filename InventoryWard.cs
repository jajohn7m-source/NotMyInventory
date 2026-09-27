using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace NotMyInventory;

/// <summary>
/// The "Inventory Ward" passive: an innate feat that shows on the player's character sheet (Feats tab)
/// and is credited in the message log whenever the mod blocks something.
/// The element row is registered in code rather than through a source sheet. If the mod is removed,
/// the game silently drops the unknown element id when loading the save.
/// </summary>
internal static class InventoryWard
{
    internal const int Id = 86420;
    internal const string Alias = "featNotMyInventory";

    internal static bool IsRegistered { get; private set; }

    internal static string Name => Plugin.PassiveName.Value;

    internal static void Register(SourceElement elements)
    {
        if (elements.map.TryGetValue(Id, out var existing))
        {
            if (existing.alias != Alias)
            {
                IsRegistered = false;
                Plugin.LogError($"Element id {Id} is already used by '{existing.alias}'; the {Name} passive is disabled (protection still works).");
                return;
            }

            ApplyText(existing);
            IsRegistered = true;
            return;
        }

        var template = elements.rows.FirstOrDefault(r => r.category == "feat" && r.tag.Contains("innate"))
                       ?? elements.rows.FirstOrDefault(r => r.category == "feat");
        if (template is null)
        {
            Plugin.LogError($"No feat found to base the {Name} passive on; the passive is disabled (protection still works).");
            return;
        }

        var row = new SourceElement.Row();
        foreach (var field in template.GetRowFields().Values)
        {
            field.SetValue(row, field.GetValue(template));
        }

        row.id = Id;
        row.alias = Alias;
        row.aliasParent = "";
        row.aliasRef = "";
        row.aliasMtp = "";
        row.type = "Feat";
        row.group = "FEAT";
        row.category = "feat";
        row.categorySub = "";
        row.tag = ["innate"];
        row.max = 1;
        row.cost = [0];
        row.req = [];
        row.proc = [];
        row.abilityType = [];
        row.foodEffect = [];
        row.langAct = [];
        row.geneSlot = 0;
        row.chance = 0;
        row.value = 0;
        row.textInc = row.textInc_JP = "";
        row.textDec = row.textDec_JP = "";
        row.levelBonus = row.levelBonus_JP = "";
        row.altname = row.altname_JP = "";
        row.textAlt = row.textAlt_JP = [];
        row.adjective = row.adjective_JP = [];
        ApplyText(row);

        elements.rows.Add(row);
        elements.map[Id] = row;
        elements.alias[Alias] = row;
        IsRegistered = true;
    }

    private static void ApplyText(SourceElement.Row row)
    {
        row.name = row.name_JP = Name;
        // The Feats list on the character sheet shows textPhase, not the name (the name is only used in the tooltip).
        row.textPhase = row.textPhase_JP = $"{Name} - your belongings cannot be stolen or destroyed";
        row.detail = row.detail_JP = "Whatever you carry stays yours. Thieves come away empty-handed, " +
                                     "and hostile magic cannot ruin a single thing in your pack.";

        // Feat tooltips show each comma-separated entry of textExtra as a bullet (max 5; ';' renders as ',').
        var bullets = new List<string>();
        if (Plugin.PreventTheft.Value)
        {
            bullets.Add("Items; food and gold cannot be stolen");
        }
        if (Plugin.PreventElementalDestruction.Value)
        {
            bullets.Add("Carried items cannot be burnt; shattered or destroyed");
        }
        if (Plugin.PreventAcid.Value)
        {
            bullets.Add("Equipment cannot be corroded by acid");
        }
        if (Plugin.PreventCurse.Value)
        {
            bullets.Add("Equipment cannot be cursed");
        }
        row.textExtra = row.textExtra_JP = string.Join(",", bullets);
    }
}

[HarmonyPatch(typeof(SourceElement), nameof(SourceElement.OnInit))]
internal static class RegisterInventoryWardPatch
{
    private static void Postfix(SourceElement __instance)
    {
        InventoryWard.Register(__instance);
    }
}

/// <summary>Grants the passive whenever a map loads (new game, loading a save, travelling), if the player lacks it.</summary>
[HarmonyPatch(typeof(Zone), nameof(Zone.Activate))]
internal static class GrantInventoryWardPatch
{
    private static void Postfix()
    {
        var pc = EClass.pc;
        if (!InventoryWard.IsRegistered || pc is null || pc.elements.ValueWithoutLink(InventoryWard.Id) > 0)
        {
            return;
        }

        pc.SetFeat(InventoryWard.Id, 1, msg: true);
    }
}
