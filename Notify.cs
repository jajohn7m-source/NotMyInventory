using System.Collections.Generic;

namespace NotMyInventory;

/// <summary>Writes the mod's messages to the in-game message log, credited to the passive.</summary>
internal static class Notify
{
    private static readonly Dictionary<string, int> LastTurnShown = new();

    internal static void Theft(Card? thief, string attempt)
    {
        var who = thief?.Name.ToTitleCase() ?? "Someone";
        Say($"{who} tried to {attempt}, but it was negated. Theft prevented by {InventoryWard.Name}.");
    }

    /// <summary>Damage messages can fire several times in one turn (e.g. every tile of a fireball), so only the first is shown.</summary>
    internal static void Damage(string source, string what = "inventory damage")
    {
        var turn = EClass.player?.stats.turns ?? 0;
        if (LastTurnShown.TryGetValue(source, out var last) && last == turn)
        {
            return;
        }

        LastTurnShown[source] = turn;
        Say($"[Incoming Damage] {source} - {what} negated by {InventoryWard.Name}.");
    }

    private static void Say(string text)
    {
        Msg.SetColor(Msg.colors.Ding);
        Msg.SayRaw(text);
    }
}
