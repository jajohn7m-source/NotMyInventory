using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace NotMyInventory;

public static class ModInfo
{
    public const string Guid = "jajohn7m.notmyinventory";
    public const string Name = "Not My Inventory";
    public const string Version = "1.0.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin? Instance;

    internal static ConfigEntry<bool> PreventTheft = null!;
    internal static ConfigEntry<bool> PreventElementalDestruction = null!;
    internal static ConfigEntry<bool> PreventAcid = null!;
    internal static ConfigEntry<bool> PreventCurse = null!;
    internal static ConfigEntry<string> PassiveName = null!;

    private void Awake()
    {
        Instance = this;

        PreventTheft = Config.Bind("Protection", "PreventTheft", true,
            "Blocks every way an NPC can take things from your inventory: steal/steal-food/steal-gold abilities " +
            "(gnomes, thieves, etc.), pickpocketing, rogues skimming gold when you swap places, and bandit encounters demanding an item.");
        PreventElementalDestruction = Config.Bind("Protection", "PreventElementalDestruction", true,
            "Fire and cold can no longer burn, shatter or cook anything you carry (including inside containers), " +
            "and fireproof/coldproof blankets are no longer used up. Also blocks any other direct damage to carried items.");
        PreventAcid = Config.Bind("Protection", "PreventAcid", true,
            "Acid can no longer corrode (lower the enchantment of) your equipment.");
        PreventCurse = Config.Bind("Protection", "PreventCurse", true,
            "Hostile curses (spells, traps, cursed scrolls) can no longer curse or doom your equipment.");
        PassiveName = Config.Bind("Display", "PassiveName", "Inventory Ward",
            "Name of the innate passive shown on your character sheet (Feats tab) and credited in the message log.");

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModInfo.Guid);
        LogInfo($"{ModInfo.Name} {ModInfo.Version} loaded");
    }

    internal static void LogDebug(object message, [CallerMemberName] string caller = "")
    {
        Instance?.Logger.LogDebug($"[{caller}] {message}");
    }

    internal static void LogInfo(object message)
    {
        Instance?.Logger.LogInfo(message);
    }

    internal static void LogError(object message)
    {
        Instance?.Logger.LogError(message);
    }
}
