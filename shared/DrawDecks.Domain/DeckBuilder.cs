#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace DrawDecks.Domain
{
public sealed class DeckValidationResult {public IReadOnlyList<string> Errors {get;} public bool IsValid=>Errors.Count==0; internal DeckValidationResult(IEnumerable<string> errors)=>Errors=errors.ToArray();}
public static class DeckBuilder {
 public static DeckValidationResult Validate(CardCatalog catalog,IEnumerable<string> ids,int minimum=1,int maximum=108){var items=(ids??throw new ArgumentNullException(nameof(ids))).ToArray();var errors=new List<string>();if(items.Length<minimum||items.Length>maximum)errors.Add("Deck size outside permitted range.");foreach(var group in items.GroupBy(x=>x,StringComparer.Ordinal)){if(!catalog.TryGet(group.Key,out var def))errors.Add("Unknown card definition: "+group.Key);else if(!def!.IsAvailable)errors.Add("Card is not available: "+group.Key);else if(group.Count()>def.MaxCopies)errors.Add("Copy limit exceeded: "+group.Key);}return new DeckValidationResult(errors);}
 public static List<CardInstance> Create(CardCatalog catalog,IEnumerable<string> ids,ulong seed){var items=ids.ToArray();var check=Validate(catalog,items);if(!check.IsValid)throw new ArgumentException(string.Join(" ",check.Errors));var deck=items.Select((id,i)=>new CardInstance("card-"+seed.ToString("x")+"-"+i,catalog.Get(id))).ToList();Shuffle(deck,seed);return deck;}
 public static void Shuffle<T>(IList<T> cards,ulong seed){var rng=new DeterministicRandom(seed);for(int i=cards.Count-1;i>0;i--){int j=rng.Next(i+1);(cards[i],cards[j])=(cards[j],cards[i]);}}
}
}
