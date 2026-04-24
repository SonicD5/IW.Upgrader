namespace IW.Upgrader.Card;

public sealed class CollectableCardType : CollectableCard.Selector {
	public required string[] CollectionIds { get; init; }
	public float BasePrice { get; init; }
}
