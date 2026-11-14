#nullable enable
using System;
namespace DrawDecks.Domain
{
public enum Suit { None=-1, Clubs=0, Diamonds=1, Hearts=2, Spades=3 }
public enum CardRarity { Common, Uncommon, Rare, Epic, Legendary }
public enum CardKind { Standard, Action, Attack, Defense, Special }
public enum CardEffect { None, DrawTwo, SkipTurn, ReverseOrder, SwapHands, StealCard, DiscardRandomCard, BlockDraw, Shield, Reflect, Negate, Wild, DeckReset, DoubleScore }
public sealed class CardDefinition {
 public string Id {get;} public string Name {get;} public CardKind Kind {get;} public CardEffect Effect {get;} public Suit Suit {get;} public int Rank {get;} public int Value {get;} public CardRarity Rarity {get;} public int MaxCopies {get;} public bool IsAvailable {get;}
 public CardDefinition(string id,string name,CardKind kind,CardEffect effect=CardEffect.None,Suit suit=Suit.None,int rank=0,int value=0,CardRarity rarity=CardRarity.Common,int maxCopies=1,bool isAvailable=true) {
 if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Card id and name are required.");
 if(!Enum.IsDefined(typeof(CardKind),kind)||!Enum.IsDefined(typeof(CardEffect),effect)||!Enum.IsDefined(typeof(CardRarity),rarity)||!Enum.IsDefined(typeof(Suit),suit)) throw new ArgumentOutOfRangeException(nameof(kind));
 if(rank<0||rank>14||value<0||maxCopies<1) throw new ArgumentOutOfRangeException(nameof(rank));
 if(kind==CardKind.Standard&&(suit==Suit.None||rank<2||rank>14)) throw new ArgumentException("Standard cards require a suit and rank 2–14.");
 Id=id;Name=name;Kind=kind;Effect=effect;Suit=suit;Rank=rank;Value=value==0?rank:value;Rarity=rarity;MaxCopies=maxCopies;IsAvailable=isAvailable;
 }}
public sealed class CardInstance : IEquatable<CardInstance> {
 public string InstanceId {get;} public CardDefinition Definition {get;} public string DefinitionId=>Definition.Id;
 public CardInstance(string id,CardDefinition definition){if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("Instance id required.");InstanceId=id;Definition=definition??throw new ArgumentNullException(nameof(definition));}
 public bool Equals(CardInstance? other)=>other!=null&&InstanceId==other.InstanceId;
 public override bool Equals(object? obj)=>Equals(obj as CardInstance);
 public override int GetHashCode()=>InstanceId.GetHashCode(StringComparison.Ordinal);
}
}
