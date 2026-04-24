// Tui/Helpers/ColoredLabel.cs
using IW.Upgrader.Card;
using Terminal.Gui;

namespace IW.Upgrader.Tui.Helpers;

/// <summary>
/// Label с посимвольной раскраской через палитру редкости.
/// </summary>
public sealed class CardLabel : View {
	private string _prefix = "";
	private string _colored = "";
	private Color _prefixColor = Color.White;
	private Color[] _palette = [Color.White];
	private Color _bg = TuiGame.BackgroundColor;

	public CardLabel() { CanFocus = false; }

	/// <summary>
	/// Белый префикс + цветной текст с палитрой редкости.
	/// </summary>
	public void SetColored(string prefix, string coloredText, CollectableCard.Rarity rarity) {
		_prefix = prefix;
		_colored = coloredText;
		_prefixColor = Color.White;
		_palette = UpgraderColors.GetColorSet(rarity);
		_bg = TuiGame.BackgroundColor;
		SetNeedsDisplay();
	}

	/// <summary>
	/// Простой белый текст (пустой слот).
	/// </summary>
	public void SetEmpty(string text) {
		_prefix = text;
		_colored = "";
		_prefixColor = Color.DarkGray;
		_palette = [Color.DarkGray];
		_bg = TuiGame.BackgroundColor;
		SetNeedsDisplay();
	}

	public override void Redraw(Rect bounds) {
		// Очистка строки
		Driver.SetAttribute(new Terminal.Gui.Attribute(_prefixColor, _bg));
		Move(0, 0);
		for (int i = 0; i < bounds.Width; i++)
			Driver.AddRune(' ');

		int col = 0;

		// ── Префикс (белый/серый) ──
		Driver.SetAttribute(new Terminal.Gui.Attribute(_prefixColor, _bg));
		for (int i = 0; i < _prefix.Length && col < bounds.Width; i++, col++) {
			Move(col, 0);
			Driver.AddRune(_prefix[i]);
		}

		// ── Цветная часть (паттерн редкости) ──
		for (int i = 0; i < _colored.Length && col < bounds.Width; i++, col++) {
			Color fg = UpgraderColors.GetPattern(_palette, i);
			Driver.SetAttribute(new Terminal.Gui.Attribute(fg, _bg));
			Move(col, 0);
			Driver.AddRune(_colored[i]);
		}
	}
}