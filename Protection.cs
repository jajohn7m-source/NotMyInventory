namespace NotMyInventory;

internal static class Protection
{
    /// <summary>The card whose inventory is protected.</summary>
    internal static bool IsProtectedOwner(Card? card)
    {
        return card is { IsPC: true };
    }

    /// <summary>True for any thing held by the player, at any depth (bags, toolbelt, equipped, etc.).</summary>
    internal static bool IsInProtectedInventory(Card? card)
    {
        return card is { isThing: true, parent: Card } && IsProtectedOwner(card.GetRootCard());
    }
}
