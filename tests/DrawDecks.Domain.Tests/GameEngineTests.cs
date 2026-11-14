using DrawDecks.Domain;
using Xunit;
namespace DrawDecks.Domain.Tests;
public sealed class GameEngineTests {
 [Fact] public void MatchCreationDealsConfiguredHandAndStoresSeed(){var seats=new[]{new SeatDefinition("human","Human",SeatKind.Human),new SeatDefinition("cpu","CPU",SeatKind.Cpu)};var state=MatchFactory.Create("match-1",123,seats);Assert.Equal(123UL,state.Seed);Assert.Equal(5,state.Players[0].Hand.Count);Assert.Equal(5,state.Players[1].Hand.Count);Assert.Equal(MatchStatus.Active,state.Status);}
 [Fact] public void RejectsMoreThanFourSeats(){var seats=Enumerable.Range(0,5).Select(i=>new SeatDefinition("p"+i,"Player "+i,SeatKind.Human)).ToArray();Assert.Throws<ArgumentOutOfRangeException>(()=>MatchFactory.Create("m",1,seats));}
 [Fact] public void RejectsOutOfTurnCommandWithoutMutation(){var seats=new[]{new SeatDefinition("p1","One",SeatKind.Human),new SeatDefinition("p2","Two",SeatKind.Cpu)};var state=MatchFactory.Create("m",7,seats);int count=state.Players[1].Hand.Count;var result=GameEngine.Draw(state,"p2");Assert.False(result.Success);Assert.Equal(ActionError.NotPlayersTurn,result.Error);Assert.Equal(count,state.Players[1].Hand.Count);}
}