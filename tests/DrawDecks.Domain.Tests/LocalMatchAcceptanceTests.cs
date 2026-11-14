using System;
using System.Linq;
using DrawDecks.Domain;
using Xunit;

namespace DrawDecks.Domain.Tests;

public sealed class LocalMatchAcceptanceTests
{
    [Fact]
    public void EveryAdvertisedLocalSeatCompositionCanCompleteAndProduceAWinner()
    {
        var configurations = new[]
        {
            new[] { SeatKind.Human, SeatKind.Cpu },
            new[] { SeatKind.Human, SeatKind.Cpu, SeatKind.Cpu },
            new[] { SeatKind.Human, SeatKind.Cpu, SeatKind.Cpu, SeatKind.Cpu },
            new[] { SeatKind.Human, SeatKind.Human, SeatKind.Cpu, SeatKind.Cpu },
            new[] { SeatKind.Human, SeatKind.Human, SeatKind.Human, SeatKind.Cpu },
            new[] { SeatKind.Human, SeatKind.Human, SeatKind.Human, SeatKind.Human }
        };

        foreach (var configuration in configurations)
        {
            var seats = configuration.Select((kind, index) =>
                new SeatDefinition("seat-" + index, "Seat " + (index + 1), kind)).ToArray();
            var catalog = CardCatalog.CreateDefault();
            var standardDeck = catalog.Definitions
                .Where(definition => definition.Kind == CardKind.Standard)
                .Select(definition => definition.Id)
                .ToArray();
            var rules = new Ruleset(startingHandSize: 1, maximumHandSize: 7,
                maximumPlayers: configuration.Length, allowStacking: false, targetScore: 20);
            var match = MatchFactory.Create(Guid.NewGuid().ToString("N"), 123456UL + (ulong)configuration.Length,
                seats, catalog, rules, standardDeck);

            for (int turn = 0; turn < 200 && match.Status == MatchStatus.Active; turn++)
            {
                var player = match.CurrentPlayer;
                var card = player.Hand
                    .Where(instance => instance.Definition.Kind == CardKind.Standard)
                    .OrderByDescending(instance => instance.Definition.Value)
                    .FirstOrDefault();

                if (card == null)
                {
                    var draw = GameEngine.Draw(match, player.Id);
                    if (draw.Success)
                        card = player.Hand
                            .Where(instance => instance.Definition.Kind == CardKind.Standard)
                            .OrderByDescending(instance => instance.Definition.Value)
                            .FirstOrDefault();
                }

                ActionResult result = card != null
                    ? GameEngine.PlayCard(match, player.Id, card.InstanceId)
                    : GameEngine.EndTurn(match, player.Id);

                Assert.True(result.Success,
                    "A legal local turn was rejected for " + configuration.Length + " seats: " + result.Error);
            }

            Assert.Equal(MatchStatus.Completed, match.Status);
            Assert.False(string.IsNullOrWhiteSpace(match.WinnerPlayerId));
            Assert.Contains(match.Players, player => player.Id == match.WinnerPlayerId);
        }
    }
}
