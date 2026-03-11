using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace DevDeckTools.Scripts.Commands;

public static class DevDeckCommandService
{
    public readonly record struct DevCommandResult(bool Success, string Message);

    public static bool CanExecute(out string reason)
    {
        reason = string.Empty;
        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null)
        {
            reason = "当前不在运行态，无法修改卡组/遗物。";
            return false;
        }

        Player? player = LocalContext.GetMe(runState);
        if (player == null)
        {
            reason = "未找到本地玩家对象，无法执行调试操作。";
            return false;
        }

        return true;
    }

    public static IReadOnlyList<string> GetCardIdSuggestions(string query, int limit = 20)
    {
        IEnumerable<CardModel> cards = ModelDb.AllCards;
        return BuildIdSuggestions(cards.Select(c => c.Id.Entry), query, limit);
    }

    public static IReadOnlyList<string> GetRelicIdSuggestions(string query, int limit = 20)
    {
        IEnumerable<RelicModel> relics = ModelDb.AllRelics;
        return BuildIdSuggestions(relics.Select(r => r.Id.Entry), query, limit);
    }

    public static async Task<DevCommandResult> AddCardToDeckAsync(string cardIdInput, int count, int upgrade)
    {
        if (!TryGetContext(out RunState? runState, out Player? player, out string reason))
        {
            return new DevCommandResult(false, reason);
        }

        if (count <= 0)
        {
            return new DevCommandResult(false, "数量必须大于 0。");
        }

        if (upgrade < 0)
        {
            return new DevCommandResult(false, "升级次数不能小于 0。");
        }

        if (!TryResolveCard(cardIdInput, out CardModel? canonicalCard, out string cardResolveMessage))
        {
            return new DevCommandResult(false, cardResolveMessage);
        }

        int successCount = 0;
        for (int i = 0; i < count; i++)
        {
            try
            {
                CardModel card = runState!.CreateCard(canonicalCard!, player!);
                for (int upgradeIndex = 0; upgradeIndex < upgrade && card.IsUpgradable; upgradeIndex++)
                {
                    card.UpgradeInternal();
                }

                card.FloorAddedToDeck = runState.TotalFloor;
                await CardPileCmd.Add(card, PileType.Deck);
                successCount++;
            }
            catch (Exception ex)
            {
                Log.Info($"[DevDeckTools] 添加卡牌失败 id={canonicalCard!.Id.Entry}, index={i + 1}/{count}, err={ex.Message}");
            }
        }

        if (successCount == 0)
        {
            return new DevCommandResult(false, $"添加卡牌失败：{canonicalCard!.Id.Entry}");
        }

        string message = $"已添加卡牌 {canonicalCard!.Id.Entry} x{successCount}";
        if (upgrade > 0)
        {
            message += $"，升级={upgrade}";
        }

        Log.Info($"[DevDeckTools] AddCard id={canonicalCard.Id.Entry}, count={count}, upgrade={upgrade}, success={successCount}");
        return new DevCommandResult(successCount == count, message);
    }

    public static async Task<DevCommandResult> RemoveCardsFromDeckAsync(string cardIdInput, int count)
    {
        if (!TryGetContext(out _, out Player? player, out string reason))
        {
            return new DevCommandResult(false, reason);
        }

        if (count <= 0)
        {
            return new DevCommandResult(false, "数量必须大于 0。");
        }

        if (!TryResolveCard(cardIdInput, out CardModel? canonicalCard, out string cardResolveMessage))
        {
            return new DevCommandResult(false, cardResolveMessage);
        }

        List<CardModel> matchedCards = player!.Deck.Cards
            .Where(card => string.Equals(card.Id.Entry, canonicalCard!.Id.Entry, StringComparison.OrdinalIgnoreCase))
            .Take(count)
            .ToList();

        if (matchedCards.Count == 0)
        {
            return new DevCommandResult(false, $"卡组中未找到卡牌：{canonicalCard!.Id.Entry}");
        }

        int removedCount = 0;
        foreach (CardModel card in matchedCards)
        {
            try
            {
                await CardPileCmd.RemoveFromDeck(card, showPreview: false);
                removedCount++;
            }
            catch (Exception ex)
            {
                Log.Info($"[DevDeckTools] 删除卡牌失败 id={card.Id.Entry}, err={ex.Message}");
            }
        }

        Log.Info($"[DevDeckTools] RemoveCard id={canonicalCard!.Id.Entry}, reqCount={count}, removed={removedCount}");
        if (removedCount == 0)
        {
            return new DevCommandResult(false, $"删除卡牌失败：{canonicalCard.Id.Entry}");
        }

        if (removedCount < count)
        {
            return new DevCommandResult(true, $"仅删除 {removedCount}/{count} 张：{canonicalCard.Id.Entry}");
        }

        return new DevCommandResult(true, $"已删除卡牌 {canonicalCard.Id.Entry} x{removedCount}");
    }

    public static async Task<DevCommandResult> AddRelicAsync(string relicIdInput)
    {
        if (!TryGetContext(out _, out Player? player, out string reason))
        {
            return new DevCommandResult(false, reason);
        }

        if (!TryResolveRelic(relicIdInput, out RelicModel? canonicalRelic, out string relicResolveMessage))
        {
            return new DevCommandResult(false, relicResolveMessage);
        }

        try
        {
            await RelicCmd.Obtain(canonicalRelic!.ToMutable(), player!);
            Log.Info($"[DevDeckTools] AddRelic id={canonicalRelic.Id.Entry}, success=true");
            return new DevCommandResult(true, $"已添加遗物：{canonicalRelic.Id.Entry}");
        }
        catch (Exception ex)
        {
            Log.Info($"[DevDeckTools] AddRelic id={canonicalRelic!.Id.Entry}, success=false, err={ex.Message}");
            return new DevCommandResult(false, $"添加遗物失败：{canonicalRelic.Id.Entry}");
        }
    }

    public static async Task<DevCommandResult> RemoveRelicAsync(string relicIdInput)
    {
        if (!TryGetContext(out _, out Player? player, out string reason))
        {
            return new DevCommandResult(false, reason);
        }

        if (string.IsNullOrWhiteSpace(relicIdInput))
        {
            return new DevCommandResult(false, "请输入遗物 ID。");
        }

        string query = relicIdInput.Trim();
        RelicModel? ownedRelic = FindOwnedRelic(player!, query);
        if (ownedRelic == null)
        {
            return new DevCommandResult(false, $"当前未持有遗物：{query}");
        }

        try
        {
            await RelicCmd.Remove(ownedRelic);
            Log.Info($"[DevDeckTools] RemoveRelic id={ownedRelic.Id.Entry}, success=true");
            return new DevCommandResult(true, $"已移除遗物：{ownedRelic.Id.Entry}");
        }
        catch (Exception ex)
        {
            Log.Info($"[DevDeckTools] RemoveRelic id={ownedRelic.Id.Entry}, success=false, err={ex.Message}");
            return new DevCommandResult(false, $"移除遗物失败：{ownedRelic.Id.Entry}");
        }
    }

    public static async Task<DevCommandResult> ApplyQuickPresetAsync()
    {
        if (!TryGetContext(out _, out Player? player, out string reason))
        {
            return new DevCommandResult(false, reason);
        }

        List<string> notes = new List<string>();
        int successSteps = 0;

        List<CardModel> sortedCards = ModelDb.AllCards.OrderBy(card => card.Id.Entry).Take(3).ToList();
        if (sortedCards.Count >= 1)
        {
            DevCommandResult addResult = await AddCardToDeckAsync(sortedCards[0].Id.Entry, 2, 1);
            notes.Add(addResult.Message);
            if (addResult.Success)
            {
                successSteps++;
            }
        }

        if (sortedCards.Count >= 2)
        {
            DevCommandResult addResult = await AddCardToDeckAsync(sortedCards[1].Id.Entry, 2, 0);
            notes.Add(addResult.Message);
            if (addResult.Success)
            {
                successSteps++;
            }
        }

        if (sortedCards.Count >= 3)
        {
            DevCommandResult addResult = await AddCardToDeckAsync(sortedCards[2].Id.Entry, 1, 0);
            notes.Add(addResult.Message);
            if (addResult.Success)
            {
                successSteps++;
            }
        }

        RelicModel? availableRelic = ModelDb.AllRelics
            .OrderBy(relic => relic.Id.Entry)
            .FirstOrDefault(relic => player!.GetRelicById(relic.Id) == null || relic.IsStackable);

        if (availableRelic != null)
        {
            DevCommandResult relicResult = await AddRelicAsync(availableRelic.Id.Entry);
            notes.Add(relicResult.Message);
            if (relicResult.Success)
            {
                successSteps++;
            }
        }

        string combinedMessage = string.Join(" | ", notes);
        bool success = successSteps > 0;
        Log.Info($"[DevDeckTools] ApplyPreset quick successSteps={successSteps}, detail={combinedMessage}");
        return new DevCommandResult(success, success
            ? $"已应用快速预设。{combinedMessage}"
            : $"快速预设执行失败。{combinedMessage}");
    }

    private static bool TryGetContext(out RunState? runState, out Player? player, out string reason)
    {
        runState = RunManager.Instance.DebugOnlyGetState();
        player = null;
        reason = string.Empty;

        if (runState == null)
        {
            reason = "当前不在运行态，无法修改卡组/遗物。";
            return false;
        }

        player = LocalContext.GetMe(runState);
        if (player == null)
        {
            reason = "未找到本地玩家对象，无法执行调试操作。";
            return false;
        }

        return true;
    }

    private static IReadOnlyList<string> BuildIdSuggestions(IEnumerable<string> allIds, string query, int limit)
    {
        string normalized = query.Trim();
        IEnumerable<string> ordered = allIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return ordered.Take(limit).ToList();
        }

        List<string> exact = ordered
            .Where(id => string.Equals(id, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<string> startsWith = ordered
            .Where(id => id.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .Where(id => !exact.Contains(id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        List<string> contains = ordered
            .Where(id => id.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .Where(id => !exact.Contains(id, StringComparer.OrdinalIgnoreCase))
            .Where(id => !startsWith.Contains(id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return exact.Concat(startsWith).Concat(contains).Take(limit).ToList();
    }

    private static bool TryResolveCard(string input, out CardModel? card, out string message)
    {
        card = null;
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            message = "请输入卡牌 ID。";
            return false;
        }

        string query = input.Trim();
        List<CardModel> candidates = ModelDb.AllCards
            .Where(c => c.Id.Entry.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Id.Entry)
            .ToList();

        card = candidates.FirstOrDefault(c => string.Equals(c.Id.Entry, query, StringComparison.OrdinalIgnoreCase))
               ?? candidates.FirstOrDefault(c => c.Id.Entry.StartsWith(query, StringComparison.OrdinalIgnoreCase))
               ?? candidates.FirstOrDefault();

        if (card == null)
        {
            message = $"未找到卡牌：{query}";
            return false;
        }

        return true;
    }

    private static bool TryResolveRelic(string input, out RelicModel? relic, out string message)
    {
        relic = null;
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            message = "请输入遗物 ID。";
            return false;
        }

        string query = input.Trim();
        List<RelicModel> candidates = ModelDb.AllRelics
            .Where(r => r.Id.Entry.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Id.Entry)
            .ToList();

        relic = candidates.FirstOrDefault(r => string.Equals(r.Id.Entry, query, StringComparison.OrdinalIgnoreCase))
                ?? candidates.FirstOrDefault(r => r.Id.Entry.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                ?? candidates.FirstOrDefault();

        if (relic == null)
        {
            message = $"未找到遗物：{query}";
            return false;
        }

        return true;
    }

    private static RelicModel? FindOwnedRelic(Player player, string input)
    {
        RelicModel? exact = player.Relics.FirstOrDefault(r => string.Equals(r.Id.Entry, input, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact;
        }

        RelicModel? startsWith = player.Relics.FirstOrDefault(r => r.Id.Entry.StartsWith(input, StringComparison.OrdinalIgnoreCase));
        if (startsWith != null)
        {
            return startsWith;
        }

        return player.Relics.FirstOrDefault(r => r.Id.Entry.Contains(input, StringComparison.OrdinalIgnoreCase));
    }
}
