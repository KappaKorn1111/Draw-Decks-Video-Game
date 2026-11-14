using System.Linq;
using DrawDecks.Domain;
using Xunit;

namespace DrawDecks.Domain.Tests;

public sealed class CustomizationTests
{
    private static CardCatalog Catalog() => CardCatalog.CreateDefault();

    [Fact]
    public void DeckEditorEnforcesAvailabilityBansAndDuplicateLimits()
    {
        var catalog = Catalog();
        var rules = new RulesetProfile("test", "Test", GameMode.Custom,
            bannedCardIds: new[] { "special-wild" }, deckMinimumSize: 1,
            deckMaximumSize: 20, duplicateCardLimit: 2);
        var editor = new DeckBuilderSession(catalog, rules, "deck-1", "My Deck");

        var definition = catalog.Get("standard-clubs-2");
        int allowedCopies = System.Math.Min(rules.DuplicateCardLimit, definition.MaxCopies);
        for (int index = 0; index < allowedCopies; index++)
            Assert.True(editor.TryAdd("standard-clubs-2", out _));
        Assert.False(editor.TryAdd("standard-clubs-2", out var duplicateError));
        Assert.Contains("limit", duplicateError);
        Assert.False(editor.TryAdd("special-wild", out var bannedError));
        Assert.Contains("banned", bannedError);
        Assert.Equal(allowedCopies, editor.CardDefinitionIds.Count);
    }

    [Fact]
    public void DeckEditorBuildsVersionedDeckAndStatistics()
    {
        var catalog = Catalog();
        var rules = new RulesetProfile("test", "Test", GameMode.Custom,
            deckMinimumSize: 2, deckMaximumSize: 20);
        var editor = new DeckBuilderSession(catalog, rules, "deck-2", "Statistics Deck");
        Assert.True(editor.TryAdd("standard-clubs-2", out _));
        Assert.True(editor.TryAdd("standard-hearts-3", out _));

        var deck = editor.Build();
        var stats = deck.GetStatistics(catalog);

        Assert.Equal(catalog.Version, deck.CatalogVersion);
        Assert.Equal(2, stats.TotalCards);
        Assert.Equal(2, stats.RecognizedCards);
        Assert.Equal(0, stats.InvalidCards);
        Assert.Equal(2, stats.RarityCounts.Values.Sum());
    }

    [Fact]
    public void CardCatalogSearchSupportsNameKindRarityAndEffectFilters()
    {
        var catalog = Catalog();
        var results = CardCatalogSearch.Search(catalog,
            new CardCatalogFilter(searchText: "shield", kind: CardKind.Defense,
                rarity: CardRarity.Uncommon, effect: CardEffect.Shield));
        Assert.Single(results);
        Assert.Equal("defense-shield", results[0].Id);
    }

    [Fact]
    public void PresetCatalogContainsAllFiveRequestedModes()
    {
        Assert.Equal(5, RulesetPresets.All.Count);
        Assert.Contains(RulesetPresets.All, item => item.Mode == GameMode.Classic);
        Assert.Contains(RulesetPresets.All, item => item.Mode == GameMode.Elimination);
        Assert.Contains(RulesetPresets.All, item => item.Mode == GameMode.TeamBattle);
        Assert.Contains(RulesetPresets.All, item => item.Mode == GameMode.Survival);
        Assert.Contains(RulesetPresets.All, item => item.Mode == GameMode.Custom);
    }

    [Fact]
    public void RulesetValidationRejectsInvalidTeamBattleSeatCount()
    {
        var catalog = Catalog();
        var team = RulesetPresets.All.Single(item => item.Mode == GameMode.TeamBattle);
        var errors = team.Validate(catalog, seatCount: 2, cpuSeatCount: 1);
        Assert.Contains(errors, error => error.Contains("Seat count"));
    }

    [Fact]
    public void LocalStoreSavesLoadsListsAndDeletesProfiles()
    {
        var catalog = Catalog();
        var deck = new DeckProfile("starter-1", "Starter", catalog.Version,
            new[] { "standard-clubs-2" }, isStarterDeck: true);
        var rules = RulesetPresets.All.Single(item => item.Mode == GameMode.Classic);
        var store = new InMemoryCustomizationStore();

        store.SaveDeck(deck);
        store.SaveRuleset(rules);

        Assert.Equal("Starter", store.LoadDeck("starter-1")?.Name);
        Assert.Equal("Classic Match", store.LoadRuleset("classic")?.Name);
        Assert.Single(store.ListDecks());
        Assert.Single(store.ListRulesets());
        Assert.True(store.DeleteDeck("starter-1"));
        Assert.True(store.DeleteRuleset("classic"));
        Assert.Null(store.LoadDeck("starter-1"));
        Assert.Null(store.LoadRuleset("classic"));
    }

    [Fact]
    public void ConfiguredMatchFactoryCreatesClassicMatchFromSavedDeckAndRules()
    {
        var catalog = Catalog();
        var ids = catalog.Definitions.Where(card => card.Id.StartsWith("standard-", System.StringComparison.Ordinal))
            .Take(20).Select(card => card.Id).ToArray();
        var deck = new DeckProfile("deck", "Classic Deck", catalog.Version, ids);
        var rules = new RulesetProfile("classic-test", "Classic Test", GameMode.Classic,
            deckMinimumSize: 20, deckMaximumSize: 40);
        var seats = new[]
        {
            new SeatDefinition("human", "Human", SeatKind.Human),
            new SeatDefinition("cpu", "CPU", SeatKind.Cpu)
        };

        var result = ConfiguredMatchFactory.Create("configured-match", 42, seats, catalog, deck, rules);

        Assert.True(result.Success, string.Join(" | ", result.Errors));
        Assert.NotNull(result.Match);
        Assert.Equal(5, result.Match!.Players[0].Hand.Count);
        Assert.Equal(5, result.Match.Players[1].Hand.Count);
    }

    [Fact]
    public void ConfiguredMatchFactoryRejectsUnsupportedRuntimeModeInsteadOfSilentlyStartingClassic()
    {
        var catalog = Catalog();
        var ids = catalog.Definitions.Where(card => card.Id.StartsWith("standard-", System.StringComparison.Ordinal))
            .Take(20).Select(card => card.Id).ToArray();
        var deck = new DeckProfile("deck", "Elimination Deck", catalog.Version, ids);
        var rules = RulesetPresets.All.Single(item => item.Mode == GameMode.Elimination);
        var seats = new[]
        {
            new SeatDefinition("human", "Human", SeatKind.Human),
            new SeatDefinition("cpu", "CPU", SeatKind.Cpu)
        };

        var result = ConfiguredMatchFactory.Create("blocked-mode", 42, seats, catalog, deck, rules);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("runtime rules are not implemented"));
    }
}
