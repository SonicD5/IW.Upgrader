using IW.Upgrader.Card;
using Terminal.Gui;

namespace IW.Upgrader.Tui.Helpers;

public sealed class CardListView : ListView {
	public enum CatalogViewMode { Cards, Packs }

	private CatalogViewMode _mode = CatalogViewMode.Cards;

	private List<CollectableCard> _cards = [];
	private List<string> _texts = [];
	private List<bool> _packAffordable = [];

	public CardListView() {
		CanFocus = true;
		AllowsMarking = false;
	}

	public void SetCards(IEnumerable<CollectableCard> cards) {
		var oldIndex = SelectedItem;

		_mode = CatalogViewMode.Cards;
		_cards = [.. cards.Where(c => c.type != null)];
		_texts = [.. _cards.Select(UpgraderColors.CardText)];
		_packAffordable.Clear();

		SetSource(_texts);
		RestoreSelection(oldIndex, _cards.Count);
	}

	public void SetPacks(IEnumerable<CollectableCardPack> packs, float balance) {
		var oldIndex = SelectedItem;

		_mode = CatalogViewMode.Packs;
		_cards.Clear();
		_packAffordable = [.. packs.Select(p => balance >= p.Price)];
		_texts = [.. packs.Select(p => $"{p.Id,-24} {p.Price,10:F2} {UpgraderGameView.Currency}")];

		SetSource(_texts);
		RestoreSelection(oldIndex, _texts.Count);
	}

	private void RestoreSelection(int oldIndex, int count) {
		if (oldIndex >= 0 && oldIndex < count)
			SelectedItem = oldIndex;
		else if (count > 0)
			SelectedItem = 0;
		else
			SelectedItem = -1;
	}

	public CollectableCard SelectedCard =>
		_mode == CatalogViewMode.Cards &&
		SelectedItem >= 0 &&
		SelectedItem < _cards.Count
			? _cards[SelectedItem]
			: default;

	public int CardCount => _mode == CatalogViewMode.Cards ? _cards.Count : 0;

	public override void Redraw(Rect contentArea) {
		var bg = TuiGame.BackgroundColor;

		// ── Корректировка TopItem: скроллить до конца ──
		int itemCount = _texts.Count;
		int visibleRows = contentArea.Height;

		// Позволяем скроллить так, чтобы последний элемент мог быть
		// на ПОСЛЕДНЕЙ строке видимой области (а не на первой)
		int maxTop = Math.Max(0, itemCount - 1);
		int minTop = Math.Max(0, itemCount - visibleRows);

		// Не даём уйти выше первого элемента
		if (TopItem < 0)
			TopItem = 0;

		// Ограничиваем снизу: последний элемент на последней видимой строке
		if (TopItem > minTop)
			TopItem = minTop;

		int top = TopItem;

		for (int row = 0; row < contentArea.Height; row++) {
			int itemIdx = top + row;
			int screenY = contentArea.Y + row;
			bool isSelected = itemIdx == SelectedItem;

			Color rowBg = isSelected
				? (HasFocus ? Color.White : Color.DarkGray)
				: bg;

			Color defaultFg = isSelected && HasFocus ? Color.Black : Color.White;

			// Очистка строки
			Driver.SetAttribute(new Terminal.Gui.Attribute(defaultFg, rowBg));
			Move(contentArea.X, screenY);
			for (int i = 0; i < contentArea.Width; i++)
				Driver.AddRune(' ');

			if (itemIdx < 0 || itemIdx >= _texts.Count)
				continue;

			string text = _texts[itemIdx];

			if (_mode == CatalogViewMode.Cards) {
				var card = _cards[itemIdx];
				var palette = UpgraderColors.GetColorSet(card.rarity);

				for (int col = 0; col < Math.Min(text.Length, contentArea.Width); col++) {
					Color fg;
					if (isSelected && HasFocus) {
						fg = UpgraderColors.GetPattern(palette, col);
					} else {
						fg = palette.Length > 1
							? UpgraderColors.GetPattern(palette, col)
							: palette[0];
					}

					Driver.SetAttribute(new Terminal.Gui.Attribute(fg, rowBg));
					Move(contentArea.X + col, screenY);
					Driver.AddRune(text[col]);
				}
			} else {
				bool canAfford = itemIdx < _packAffordable.Count && _packAffordable[itemIdx];

				Color packFg;
				if (isSelected && HasFocus)
					packFg = Color.Black;
				else if (isSelected)
					packFg = Color.White;
				else
					packFg = canAfford ? Color.BrightGreen : Color.DarkGray;

				for (int col = 0; col < Math.Min(text.Length, contentArea.Width); col++) {
					Driver.SetAttribute(new Terminal.Gui.Attribute(packFg, rowBg));
					Move(contentArea.X + col, screenY);
					Driver.AddRune(text[col]);
				}
			}
		}
	}
}