#nullable enable
using System;
using System.Linq;
namespace DrawDecks.Domain
{
public sealed class ActionResult {public bool Success{get;} public ActionError Error{get;} public MatchState State{get;} internal ActionResult(bool ok,ActionError error,MatchState state){Success=ok;Error=error;State=state;}}
public static partial class GameEngine {
 public static ActionResult Draw(MatchState state,string playerId,int count=1)
 {
  var err=ValidateDraw(state,playerId,count);
  if(err!=ActionError.None)return Fail(state,err);
  var p=state.CurrentPlayer;
  if(state.BlockedDrawPlayerId==playerId)
  {
   state.Emit("CommandAccepted",playerId,data:"Draw|"+count);
   state.BlockedDrawPlayerId=null;
   state.Emit("DrawBlocked",playerId);
   return Ok(state);
  }
  state.Emit("CommandAccepted",playerId,data:"Draw|"+count);
  for(int i=0;i<count;i++)DrawOne(state,p);
  state.Emit("CardsDrawn",playerId,data:"count="+count);
  return Ok(state);
 }
 public static ActionResult PlayCard(MatchState state,string playerId,string cardId,string? targetPlayerId=null,Suit chosenSuit=Suit.None)
 {
  var err=ValidatePlayCard(state,playerId,cardId,targetPlayerId,chosenSuit);
  if(err!=ActionError.None)return Fail(state,err);
  var p=state.CurrentPlayer;
  var card=p.MutableHand.First(x=>x.InstanceId==cardId);
  var target=targetPlayerId==null?null:state.MutablePlayers.First(x=>x.Id==targetPlayerId&&!x.IsEliminated);
  state.Emit("CommandAccepted",playerId,card.InstanceId,targetPlayerId,"PlayCard|"+chosenSuit);
  if(card.Definition.Effect==CardEffect.None)state.TurnHadScoringAction=true;
  p.MutableHand.Remove(card);
  state.MutableDiscardPile.Add(card);
  state.Emit("CardPlayed",playerId,card.InstanceId,targetPlayerId,card.Definition.Effect.ToString());
  Resolve(state,p,card,target);
  if(card.Definition.Effect==CardEffect.Wild)state.ActiveSuit=chosenSuit;
  if(state.PendingAttack==null)Advance(state);
  CheckVictory(state);
  return Ok(state);
 }
 public static ActionResult RespondToAttack(MatchState state,string playerId,string? defenseCardId)
 {
  var err=ValidateRespondToAttack(state,playerId,defenseCardId);
  if(err!=ActionError.None)return Fail(state,err);
  var attack=state.PendingAttack!;
  var defender=state.CurrentPlayer;
  if(defenseCardId!=null)
  {
   var defense=defender.MutableHand.First(x=>x.InstanceId==defenseCardId);
   state.Emit("CommandAccepted",playerId,defense.InstanceId,attack.AttackerId,"RespondToAttack|"+defenseCardId);
   defender.MutableHand.Remove(defense);
   state.MutableDiscardPile.Add(defense);
   if(defense.Definition.Effect==CardEffect.Reflect)ApplyAttack(state,attack.AttackerId,attack.Effect,playerId);
   state.Emit(defense.Definition.Effect==CardEffect.Reflect?"AttackReflected":"AttackNegated",playerId,defense.InstanceId,attack.AttackerId);
  }
  else
  {
   state.Emit("CommandAccepted",playerId,null,attack.AttackerId,"RespondToAttack|-");
   ApplyAttack(state,attack.TargetId,attack.Effect,attack.AttackerId);
  }
  state.PendingAttack=null;
  int attackerIndex=state.MutablePlayers.FindIndex(player=>player.Id==attack.AttackerId);
  if(attackerIndex<0)throw new InvalidOperationException("Pending attack references a missing attacker.");
  state.CurrentPlayerIndex=attackerIndex;
  Advance(state);
  CheckVictory(state);
  return Ok(state);
 }
 public static ActionResult Discard(MatchState state,string playerId,string cardId){var err=ValidateTurn(state,playerId);if(err!=ActionError.None)return Fail(state,err);var p=state.CurrentPlayer;var card=p.MutableHand.FirstOrDefault(c=>c.InstanceId==cardId);if(card==null)return Fail(state,ActionError.CardNotInHand);state.Emit("CommandAccepted",playerId,card.InstanceId,data:"Discard");p.MutableHand.Remove(card);state.MutableDiscardPile.Add(card);state.Emit("CardDiscarded",playerId,card.InstanceId);Advance(state);CheckVictory(state);return Ok(state);}
 public static ActionResult EliminatePlayer(MatchState state,string actorId,string targetPlayerId){var err=ValidateTurn(state,actorId);if(err!=ActionError.None)return Fail(state,err);var target=state.MutablePlayers.FirstOrDefault(p=>p.Id==targetPlayerId&&!p.IsEliminated);if(target==null||target.Id==actorId)return Fail(state,ActionError.InvalidTarget);if(state.Rules.Mode==GameMode.TeamBattle&&target.TeamId!=null&&target.TeamId==state.CurrentPlayer.TeamId)return Fail(state,ActionError.InvalidTarget);state.Emit("CommandAccepted",actorId,null,target.Id,"EliminatePlayer");target.IsEliminated=true;target.Health=0;state.Emit("PlayerEliminated",actorId,null,target.Id);if(state.Rules.Mode==GameMode.Survival&&target.Kind==SeatKind.Cpu){state.CpuDefeatedThisWave++;state.SurvivalScore+=state.Rules.SurvivalScorePerCpu;}state.PendingSkipPlayerIds.Remove(target.Id);CheckVictory(state);if(state.Status==MatchStatus.Active)Advance(state);return Ok(state);}
 public static ActionResult EndTurn(MatchState state,string playerId)
 {
  var err=ValidateEndTurn(state,playerId);
  if(err!=ActionError.None)return Fail(state,err);
  state.Emit("CommandAccepted",playerId,data:"EndTurn");
  Advance(state);
  CheckVictory(state);
  return Ok(state);
 }
 private static void Resolve(MatchState s,PlayerState p,CardInstance c,PlayerState? target){switch(c.Definition.Effect){
 case CardEffect.None:AwardScore(s,p,c.Definition.Value*(s.DoubleScoreActive?2:1));s.DoubleScoreActive=false;break;
 case CardEffect.DrawTwo:for(int i=0;i<2&&p.MutableHand.Count<s.Rules.MaximumHandSize&&s.DrawPile.Count>0;i++)DrawOne(s,p);break;
 case CardEffect.SkipTurn:if(target!=null)s.PendingSkipPlayerIds.Add(target.Id);s.Emit("SkipQueued",p.Id,c.InstanceId,target?.Id);break;
 case CardEffect.ReverseOrder:s.Direction*=-1;break;
 case CardEffect.SwapHands:if(target!=null){var temp=p.MutableHand.ToArray();p.MutableHand.Clear();p.MutableHand.AddRange(target.MutableHand);target.MutableHand.Clear();target.MutableHand.AddRange(temp);}break;
 case CardEffect.StealCard:case CardEffect.DiscardRandomCard:case CardEffect.BlockDraw:case CardEffect.Damage:s.PendingAttack=new PendingAttack(p.Id,target!.Id,c.Definition.Effect,c.InstanceId);s.CurrentPlayerIndex=s.MutablePlayers.IndexOf(target);break;
 case CardEffect.Wild:break;
 case CardEffect.DeckReset:if(s.MutableDiscardPile.Count>0){var recycled=s.MutableDiscardPile.ToList();s.MutableDiscardPile.Clear();DeckBuilder.Shuffle(recycled,NextRandom(s));s.DrawPile.AddRange(recycled);}break;
 case CardEffect.DoubleScore:s.DoubleScoreActive=true;break;
 case CardEffect.Shield:case CardEffect.Reflect:case CardEffect.Negate:break;
 default:throw new InvalidOperationException("Unimplemented effect "+c.Definition.Effect);
 }}
 private static void ApplyAttack(MatchState s,string targetId,CardEffect effect,string attackerId){var target=s.MutablePlayers.First(x=>x.Id==targetId);switch(effect){case CardEffect.StealCard:if(target.MutableHand.Count>0){var card=target.MutableHand[0];target.MutableHand.RemoveAt(0);var attacker=s.MutablePlayers.First(x=>x.Id==attackerId);if(attacker.MutableHand.Count<s.Rules.MaximumHandSize)attacker.MutableHand.Add(card);else s.MutableDiscardPile.Add(card);}break;case CardEffect.DiscardRandomCard:if(target.MutableHand.Count>0){int i=(int)(NextRandom(s)%(ulong)target.MutableHand.Count);var card=target.MutableHand[i];target.MutableHand.RemoveAt(i);s.MutableDiscardPile.Add(card);}break;case CardEffect.BlockDraw: s.BlockedDrawPlayerId=targetId;break;case CardEffect.Damage: if(s.Rules.Mode==GameMode.Elimination||s.Rules.Mode==GameMode.TeamBattle||s.Rules.Mode==GameMode.Survival){target.Health=Math.Max(0,target.Health-Math.Max(1,s.Rules.DamagePerHit));s.Emit("PlayerDamaged",attackerId,null,targetId,"damage="+Math.Max(1,s.Rules.DamagePerHit)+";health="+target.Health);if(target.Health==0){target.IsEliminated=true;s.PendingSkipPlayerIds.Remove(target.Id);s.Emit("PlayerEliminated",attackerId,null,targetId,"health-depleted");if(s.Rules.Mode==GameMode.Survival&&target.Kind==SeatKind.Cpu){s.CpuDefeatedThisWave++;s.SurvivalScore+=s.Rules.SurvivalScorePerCpu;}}}break;}s.Emit("AttackResolved",attackerId,null,targetId,effect.ToString());}
 private static bool IsAttack(CardEffect e)=>e==CardEffect.StealCard||e==CardEffect.DiscardRandomCard||e==CardEffect.BlockDraw||e==CardEffect.Damage;
 private static void DrawOne(MatchState s,PlayerState p){if(s.DrawPile.Count==0)return;var c=s.DrawPile[0];s.DrawPile.RemoveAt(0);p.MutableHand.Add(c);}
 private static void Advance(MatchState s)
 {
  if(s.Status!=MatchStatus.Active)return;

  var completingPlayer=s.CurrentPlayer;
  int previousStreak=s.ConsecutiveNonScoringTurns.TryGetValue(completingPlayer.Id,out var value)?value:0;
  s.ConsecutiveNonScoringTurns[completingPlayer.Id]=s.TurnHadScoringAction?0:previousStreak+1;
  s.TurnHadScoringAction=false;

  var livingPlayers=s.MutablePlayers.Where(player=>!player.IsEliminated).ToArray();
  bool deckExhaustedAndStalled=s.DrawPile.Count==0&&livingPlayers.Length>0&&livingPlayers.All(player=>
   s.ConsecutiveNonScoringTurns.TryGetValue(player.Id,out var count)&&count>=2);
  bool turnLimitReached=s.TurnNumber>=s.Rules.MaximumTurns;

  if(s.Rules.Mode!=GameMode.Survival&&(deckExhaustedAndStalled||turnLimitReached))
  {
   CheckVictory(s);
   if(s.Status==MatchStatus.Active)
    ResolveSuddenScore(s,turnLimitReached?"maximum-turn-limit":"deck-exhausted-and-stalled");
   if(s.Status!=MatchStatus.Active)return;
  }

  int seatCount=s.MutablePlayers.Count;
  int livingCount=s.MutablePlayers.Count(player=>!player.IsEliminated);
  int origin=s.CurrentPlayerIndex;
  int index=origin;

  for(int step=1;step<=seatCount;step++)
  {
   index=(index+s.Direction+seatCount)%seatCount;
   var candidate=s.MutablePlayers[index];
   if(candidate.IsEliminated)continue;

   bool isOrigin=index==origin;
   bool isSkipped=s.PendingSkipPlayerIds.Remove(candidate.Id);
   if(isSkipped&&!isOrigin&&livingCount>1)
   {
    s.Emit("TurnSkipped",candidate.Id);
    livingCount--;
    continue;
   }

   // Never skip every living seat. The last eligible seat becomes the
   // deterministic progress seat when every other living seat is queued.
   break;
  }

  s.CurrentPlayerIndex=index;
  s.TurnNumber++;
  s.Emit("TurnStarted",s.CurrentPlayer.Id,data:"turn="+s.TurnNumber);
 }

 private static void ResolveSuddenScore(MatchState s,string reason)
 {
  s.Emit("SuddenScoreStarted","system",data:reason);
  var living=s.MutablePlayers.Where(player=>!player.IsEliminated).ToArray();
  if(s.Rules.Mode==GameMode.Elimination)
  {
   var ranked=living.OrderByDescending(player=>player.Health).ThenByDescending(player=>player.Score)
    .ThenByDescending(player=>player.Hand.Count).ToArray();
   if(ranked.Length==0){s.Status=MatchStatus.Draw;s.Emit("MatchDraw","system",data:"sudden-score-no-living-players");return;}
   if(ranked.Length>1&&ranked[0].Health==ranked[1].Health&&ranked[0].Score==ranked[1].Score&&ranked[0].Hand.Count==ranked[1].Hand.Count)
   {s.Status=MatchStatus.Draw;s.Emit("MatchDraw","system",data:"elimination-health-tie");return;}
   s.Status=MatchStatus.Completed;s.WinnerPlayerId=ranked[0].Id;
   s.Emit("MatchWon",ranked[0].Id,data:"elimination-health-sudden-score");return;
  }
  if(s.Rules.Mode==GameMode.TeamBattle)
  {
   var teams=living.Where(player=>player.TeamId!=null).GroupBy(player=>player.TeamId!,StringComparer.Ordinal)
    .Select(group=>new {Id=group.Key,Score=s.MutableTeamScores.TryGetValue(group.Key,out var score)?score:0,
     Health=group.Sum(player=>player.Health),Hand=group.Sum(player=>player.Hand.Count)})
    .OrderByDescending(team=>team.Score).ThenByDescending(team=>team.Health).ThenByDescending(team=>team.Hand).ToArray();
   if(teams.Length==0){s.Status=MatchStatus.Draw;s.Emit("MatchDraw","system",data:"sudden-score-no-living-teams");return;}
   if(teams.Length>1&&teams[0].Score==teams[1].Score&&teams[0].Health==teams[1].Health&&teams[0].Hand==teams[1].Hand)
   {s.Status=MatchStatus.Draw;s.Emit("MatchDraw","system",data:"team-sudden-score-tie");return;}
   s.Status=MatchStatus.Completed;s.WinnerTeamId=teams[0].Id;
   s.Emit("TeamWon","system",data:"team="+teams[0].Id+";sudden-score");return;
  }
  var leaders=living.OrderByDescending(player=>player.Score)
   .ThenByDescending(player=>player.Hand.Count).ToArray();
  if(leaders.Length==0)
  {
   s.Status=MatchStatus.Draw;
   s.Emit("MatchDraw","system",data:"sudden-score-no-living-players");
   return;
  }

  if(leaders.Length>1&&leaders[0].Score==leaders[1].Score&&leaders[0].Hand.Count==leaders[1].Hand.Count)
  {
   s.Status=MatchStatus.Draw;
   s.Emit("MatchDraw","system",data:"sudden-score-tie");
   return;
  }

  s.Status=MatchStatus.Completed;
  s.WinnerPlayerId=leaders[0].Id;
  s.Emit("MatchWon",leaders[0].Id,data:"sudden-score");
 }
 private static void AwardScore(MatchState state, PlayerState player, int points)
 {
  if(points<=0)return;
  player.Score+=points;
  if(state.Rules.Mode==GameMode.TeamBattle&&player.TeamId!=null)
   state.MutableTeamScores[player.TeamId]=state.MutableTeamScores.TryGetValue(player.TeamId,out var score)?score+points:points;
  if(state.Rules.Mode==GameMode.Survival&&player.Kind==SeatKind.Human)
   state.SurvivalScore+=points;
 }
 private static void AdvanceSurvivalWave(MatchState state)
 {
  var human=state.MutablePlayers.FirstOrDefault(player=>player.Kind==SeatKind.Human);
  if(human==null||human.IsEliminated)
  {
   state.Status=MatchStatus.Completed;
   state.Emit("SurvivalEnded","system",data:"player-defeated;score="+state.SurvivalScore+";wave="+state.SurvivalWave);
   return;
  }
  var cpus=state.MutablePlayers.Where(player=>player.Kind==SeatKind.Cpu).ToArray();
  if(cpus.Length==0)return;
  if(cpus.All(player=>player.IsEliminated))
  {
   state.Emit("SurvivalWaveCleared",human.Id,data:"wave="+state.SurvivalWave+";score="+state.SurvivalScore);
   if(state.Rules.SurvivalMaximumWaves>0&&state.SurvivalWave>=state.Rules.SurvivalMaximumWaves)
   {
    state.Status=MatchStatus.Completed;
    state.WinnerPlayerId=human.Id;
    state.Emit("SurvivalCompleted",human.Id,data:"waves="+state.SurvivalWave+";score="+state.SurvivalScore);
    return;
   }
   state.SurvivalWave++;
   state.CpuDefeatedThisWave=0;
   foreach(var cpu in cpus)
   {
    cpu.IsEliminated=false;
    cpu.Health=state.Rules.StartingHealth+((state.SurvivalWave-1)*2);
   }
   state.Emit("SurvivalWaveStarted",human.Id,data:"wave="+state.SurvivalWave+";cpuHealth="+cpus[0].Health);
  }
 }
 private static void CheckVictory(MatchState s)
 {
  if(s.Status!=MatchStatus.Active)return;
  if(s.Rules.Mode==GameMode.Survival){AdvanceSurvivalWave(s);return;}
  var alive=s.MutablePlayers.Where(p=>!p.IsEliminated).ToArray();
  if(s.Rules.Mode==GameMode.TeamBattle)
  {
   var aliveTeams=alive.Where(p=>p.TeamId!=null).Select(p=>p.TeamId!).Distinct(StringComparer.Ordinal).ToArray();
   if(aliveTeams.Length==1){s.Status=MatchStatus.Completed;s.WinnerTeamId=aliveTeams[0];s.Emit("TeamWon",alive[0].Id,data:"team="+aliveTeams[0]);return;}
   if(aliveTeams.Length==0){s.Status=MatchStatus.Draw;s.Emit("MatchDraw","system");return;}
   var winners=s.MutableTeamScores.Where(pair=>pair.Value>=s.Rules.TargetScore).OrderByDescending(pair=>pair.Value).ToArray();
   if(winners.Length>0)
   {
    if(winners.Length>1&&winners[0].Value==winners[1].Value){s.Status=MatchStatus.Draw;s.Emit("MatchDraw","system",data:"team-score-tie");}
    else{s.Status=MatchStatus.Completed;s.WinnerTeamId=winners[0].Key;s.Emit("TeamWon","system",data:"team="+winners[0].Key+";score="+winners[0].Value);}
   }
   return;
  }
  if(alive.Length==1){s.Status=MatchStatus.Completed;s.WinnerPlayerId=alive[0].Id;s.Emit("MatchWon",alive[0].Id);}
  else if(alive.Length==0){s.Status=MatchStatus.Draw;s.Emit("MatchDraw","system");}
  else if(s.Rules.Mode==GameMode.Elimination)return;
  else
  {
   var leaders=alive.Where(p=>p.Score>=s.Rules.TargetScore).OrderByDescending(p=>p.Score).ToArray();
   if(leaders.Length>0){s.Status=MatchStatus.Completed;if(leaders.Length>1&&leaders[0].Score==leaders[1].Score){s.Status=MatchStatus.Draw;s.Emit("MatchDraw","system",data:"score-tie");}else{s.WinnerPlayerId=leaders[0].Id;s.Emit("MatchWon",leaders[0].Id,data:"target-score");}}
  }
 }
 private static ActionError ValidateTurn(MatchState s,string id){if(s==null)throw new ArgumentNullException(nameof(s));if(s.Status!=MatchStatus.Active)return ActionError.MatchNotActive;if(s.PendingAttack!=null)return ActionError.InvalidPhase;if(s.CurrentPlayer.Id!=id)return s.MutablePlayers.Any(p=>p.Id==id)?ActionError.NotPlayersTurn:ActionError.InvalidPlayer;return ActionError.None;}
 private static ulong NextRandom(MatchState s){ulong x=s.RandomState;x^=x>>12;x^=x<<25;x^=x>>27;s.RandomState=x;return x*2685821657736338717UL;}
 private static ActionResult Ok(MatchState s)=>new(true,ActionError.None,s);private static ActionResult Fail(MatchState s,ActionError e)=>new(false,e,s);
}
}
