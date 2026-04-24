using SonicD5.Json;

namespace IW.Upgrader.Card;

public sealed class CollectableCardPack {
	[JsonSerializable("card_set")]
	private readonly CollectableCard.Selector[] _cardSelectorSet = [];
	public required string Id { get; init; }
	public float Price { get; init; }
	[JsonSerializable("")]
	[JsonSerializationIgnore]
	public IReadOnlyList<CollectableCard> CardSet { get; private set; } = [];

	public void InitCardSet(IEnumerable<CollectableCard> ctg) => CardSet = [.. _cardSelectorSet.SelectMany(s => ctg.Where(i => i.type.Id == s.Id && i.rarity >= s.MinRarity && i.rarity <= s.MaxRarity))];

	public override string ToString() => $"Id: {Id}, Price: {Price}";
}