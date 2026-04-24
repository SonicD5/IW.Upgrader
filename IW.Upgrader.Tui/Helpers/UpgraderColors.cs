using IW.Upgrader.Card;
using System.Runtime.CompilerServices;
using Terminal.Gui;

namespace IW.Upgrader.Tui.Helpers;

public static class UpgraderColors {

	public static Color[] GetColorSet(this CollectableCard.Rarity rarity) => rarity switch {
		CollectableCard.Rarity.Common => [Color.Gray],
		CollectableCard.Rarity.Uncommon => [Color.BrightGreen],
		CollectableCard.Rarity.Rare => [Color.BrightBlue],
		CollectableCard.Rarity.Epic => [Color.BrightMagenta],
		CollectableCard.Rarity.Gold => [Color.BrightYellow],
		CollectableCard.Rarity.CromicThirdTier => [Color.BrightRed, Color.Red],
		CollectableCard.Rarity.CromicSecondTier => [Color.BrightCyan, Color.Cyan],
		CollectableCard.Rarity.CromicFirstTier => [Color.BrightRed, Color.BrightYellow,
		Color.BrightGreen, Color.BrightCyan, Color.BrightBlue, Color.BrightMagenta],
		CollectableCard.Rarity.MasterDrop => [Color.DarkGray, Color.Gray, Color.White],
		_ => [Color.White],
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color GetPattern(Color[] palette, int charIndex) =>
		palette[charIndex % palette.Length];

	public static ColorScheme MakeScheme(Color fg, Color bg) {
		var norm = new Terminal.Gui.Attribute(fg, bg);
		var focus = new Terminal.Gui.Attribute(bg, fg);
		return new ColorScheme {
			Normal = norm,
			Focus = focus,
			HotNormal = norm,
			HotFocus = focus,
			Disabled = new Terminal.Gui.Attribute(Color.DarkGray, bg)
		};
	}

	public static string RarityLabel(CollectableCard.Rarity r) => r switch {
		CollectableCard.Rarity.CromicThirdTier => "Cromic-★☆☆",
		CollectableCard.Rarity.CromicSecondTier => "Cromic-★★☆",
		CollectableCard.Rarity.CromicFirstTier => "Cromic-★★★",
		CollectableCard.Rarity.MasterDrop => "Master-Drop",
		_ => r.ToString()
	};

	public static string CardText(CollectableCard c) =>
		$"{c.type.Id,-20} [{RarityLabel(c.rarity),-11}] {c.price,9:F2} {UpgraderGameView.Currency}";
}