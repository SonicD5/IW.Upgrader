using IW.Upgrader.Card;

namespace IW.Upgrader; 

public sealed class UpgradeInfo {
	public required CollectableCard[] InputItems { get; init; }
	public CollectableCard DropItem { get; init; }
	public float Chance { get; init; }
	public bool Result { get; init; }
}
