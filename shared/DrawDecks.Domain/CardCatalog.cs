#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace DrawDecks.Domain
{
public sealed class CardCatalog {
 readonly Dictionary<string,CardDefinition> definitions; readonly List<CardDefinition> orderedDefinitions=new();
 public string Version {get;} public IReadOnlyCollection<CardDefinition> Definitions=>orderedDefinitions.AsReadOnly();
 public CardCatalog(string version,IEnumerable<CardDefinition> items)
 {
  if(string.IsNullOrWhiteSpace(version))throw new ArgumentException("Version required.",nameof(version));
  if(items==null)throw new ArgumentNullException(nameof(items));
  Version=version;
  definitions=new(StringComparer.Ordinal);
  foreach(var item in items)
  {
   if(item==null)throw new ArgumentException("Catalog cannot contain null definitions.",nameof(items));
   if(!definitions.TryAdd(item.Id,item))throw new ArgumentException("Duplicate definition: "+item.Id);
   orderedDefinitions.Add(item);
  }
  if(definitions.Count==0)throw new ArgumentException("Catalog cannot be empty.",nameof(items));
 }
 public CardDefinition Get(string id)
 {
  if(string.IsNullOrWhiteSpace(id)||!definitions.TryGetValue(id,out var value))
   throw new KeyNotFoundException("Unknown card: "+id);
  return value;
 }
 public bool TryGet(string id,out CardDefinition? value)
 {
  if(string.IsNullOrWhiteSpace(id)){value=null;return false;}
  return definitions.TryGetValue(id,out value);
 }
 public static CardCatalog CreateDefault(){var list=new List<CardDefinition>();foreach(Suit suit in new[]{Suit.Clubs,Suit.Diamonds,Suit.Hearts,Suit.Spades})for(int rank=2;rank<=14;rank++)list.Add(new CardDefinition("standard-"+suit.ToString().ToLowerInvariant()+"-"+rank,rank+" of "+suit,CardKind.Standard,suit:suit,rank:rank));Add("action-draw-2","Draw 2",CardKind.Action,CardEffect.DrawTwo,CardRarity.Uncommon);Add("action-skip","Skip Turn",CardKind.Action,CardEffect.SkipTurn,CardRarity.Uncommon);Add("action-reverse","Reverse Order",CardKind.Action,CardEffect.ReverseOrder,CardRarity.Rare);Add("action-swap","Swap Hands",CardKind.Action,CardEffect.SwapHands,CardRarity.Epic);Add("attack-steal","Steal Card",CardKind.Attack,CardEffect.StealCard,CardRarity.Rare);Add("attack-discard","Discard Random Card",CardKind.Attack,CardEffect.DiscardRandomCard,CardRarity.Rare);Add("attack-block-draw","Block Draw",CardKind.Attack,CardEffect.BlockDraw,CardRarity.Uncommon);Add("attack-damage","Strike",CardKind.Attack,CardEffect.Damage,CardRarity.Rare,5,4);Add("defense-shield","Shield",CardKind.Defense,CardEffect.Shield,CardRarity.Uncommon);Add("defense-reflect","Reflect",CardKind.Defense,CardEffect.Reflect,CardRarity.Epic);Add("defense-negate","Negate",CardKind.Defense,CardEffect.Negate,CardRarity.Legendary);Add("special-wild","Wild Card",CardKind.Special,CardEffect.Wild,CardRarity.Rare);Add("special-reset","Deck Reset",CardKind.Special,CardEffect.DeckReset,CardRarity.Epic);Add("special-double","Double Score",CardKind.Special,CardEffect.DoubleScore,CardRarity.Legendary);return new CardCatalog("1.0.0",list);
 void Add(string id,string name,CardKind kind,CardEffect effect,CardRarity rarity,int value=0,int maxCopies=1)=>list.Add(new CardDefinition(id,name,kind,effect,value:value,rarity:rarity,maxCopies:maxCopies));}
}
}
