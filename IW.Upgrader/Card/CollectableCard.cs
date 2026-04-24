namespace IW.Upgrader.Card; 
public readonly struct CollectableCard(CollectableCardType type, CollectableCard.Rarity rarity) {
	public enum Rarity { Common, Uncommon, Rare, Epic, Gold, CromicThirdTier, CromicSecondTier, CromicFirstTier, MasterDrop }
	public const float RarityExponent = 3.3f;

	public readonly CollectableCardType type = type;
	public readonly Rarity rarity = rarity;
	public bool IsPurchaseable => rarity != Rarity.MasterDrop && rarity <= type.MaxRarity && rarity >= type.MinRarity;

	public readonly float price = rarity == Rarity.MasterDrop ? type.BasePrice : type.BasePrice * MathF.Pow(RarityExponent, (float)rarity);

	public override string ToString() => $"Id: \"{type.Id}\", Rarity: {rarity}, Price: {price}, CollectionIds: [{string.Join(", ", type.CollectionIds.Select(id => $"\"{id}\""))}]";

	public class Selector {
		public required string Id { get; init; }
		public Rarity MinRarity { get; init; } = Rarity.Common;
		public Rarity MaxRarity { get; init; } = Rarity.CromicFirstTier;
	}
}
