#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawDecks.Domain
{
    public static class MatchFactory
    {
        public static MatchState Create(string matchId, ulong seed, IReadOnlyList<SeatDefinition> seats,
            CardCatalog? catalog = null, Ruleset? rules = null, IEnumerable<string>? deckDefinitionIds = null)
        {
            if (string.IsNullOrWhiteSpace(matchId)) throw new ArgumentException("Match id required.");
            if (seats == null) throw new ArgumentNullException(nameof(seats));
            catalog ??= CardCatalog.CreateDefault();
            rules ??= Ruleset.Default;
            if (seats.Count < 2 || seats.Count > rules.MaximumPlayers)
                throw new ArgumentOutOfRangeException(nameof(seats), "Matches require 2–4 seats within the selected ruleset.");
            if (seats.Any(s => s == null) || !seats.Any(s => s.Kind == SeatKind.Human))
                throw new ArgumentException("At least one human seat required.");
            if (seats.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != seats.Count)
                throw new ArgumentException("Seat ids must be unique.");

            var configuredSeats = seats.Select((seat, index) =>
            {
                if (rules.Mode == GameMode.TeamBattle)
                    return new SeatDefinition(seat.Id, seat.DisplayName, seat.Kind,
                        seat.TeamId ?? (index % 2 == 0 ? "team-a" : "team-b"));
                return new SeatDefinition(seat.Id, seat.DisplayName, seat.Kind, seat.TeamId);
            }).ToList();

            if (rules.Mode == GameMode.TeamBattle &&
                (configuredSeats.Count != 4 || configuredSeats.Select(seat => seat.TeamId).Distinct(StringComparer.Ordinal).Count() != 2 ||
                 configuredSeats.GroupBy(seat => seat.TeamId, StringComparer.Ordinal).Any(team => team.Count() != 2)))
                throw new ArgumentException("Team Battle requires four seats split into two teams of two.", nameof(seats));
            if (rules.Mode == GameMode.Survival &&
                (configuredSeats.Count(seat => seat.Kind == SeatKind.Human) != 1 ||
                 configuredSeats.Count(seat => seat.Kind == SeatKind.Cpu) < 1))
                throw new ArgumentException("Survival requires one human survivor and at least one CPU opponent.", nameof(seats));

            var ids = deckDefinitionIds?.ToArray() ?? catalog.Definitions.Select(d => d.Id).ToArray();
            var deck = DeckBuilder.Create(catalog, ids, seed);
            if (deck.Count < configuredSeats.Count * rules.StartingHandSize)
                throw new ArgumentException("Deck too small for starting hands.");
            var players = configuredSeats.Select(seat => new PlayerState(seat, rules.StartingHealth)).ToList();
            foreach (var player in players)
                for (int i = 0; i < rules.StartingHandSize; i++)
                {
                    player.MutableHand.Add(deck[0]);
                    deck.RemoveAt(0);
                }

            var state = new MatchState(matchId, seed, catalog.Version, rules, players, deck,
                configuredSeats, ids.ToList());
            state.Emit("MatchCreated", players[0].Id, data:"seed=" + seed + ";mode=" + rules.Mode);
            state.Emit("CardsDealt", players[0].Id, data:"hand=" + rules.StartingHandSize);
            if (rules.Mode == GameMode.TeamBattle)
                state.Emit("TeamsAssigned", "system", data:string.Join(",", players.Select(player => player.Id + ":" + player.TeamId)));
            if (rules.Mode == GameMode.Survival)
                state.Emit("SurvivalWaveStarted", players.First(player => player.Kind == SeatKind.Human).Id, data:"wave=1");
            return state;
        }
    }
}
