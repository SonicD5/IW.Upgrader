using IW.Upgrader.Card;
using Terminal.Gui;

namespace IW.Upgrader.Tui.Helpers;

internal sealed class HistoryListView : View {
	private List<UpgradeInfo> _allItems = [];
	private List<UpgradeInfo> _pageItems = [];
	private int _topItem;
	private int _selectedItem;
	private int _currentPage;
	private int _totalPages;

	public const int ItemsPerPage = 50;

	// Внешние элементы для обновления
	private Label? _pageLabel;

	public HistoryListView() { CanFocus = true; }

	public void SetItems(List<UpgradeInfo> items) {
		_allItems = items;
		_totalPages = Math.Max(1, (int)Math.Ceiling((double)items.Count / ItemsPerPage));
		SetPage(0);
	}

	public void SetPageLabel(Label label) => _pageLabel = label;

	public void SetPage(int page) {
		_currentPage = Math.Clamp(page, 0, _totalPages - 1);
		_pageItems = [.. _allItems
			.Skip(_currentPage * ItemsPerPage)
			.Take(ItemsPerPage)];
		_topItem = 0;
		_selectedItem = 0;
		UpdatePageLabel();
		SetNeedsDisplay();
	}

	public void NextPage() { if (_currentPage < _totalPages - 1) SetPage(_currentPage + 1); }
	public void PrevPage() { if (_currentPage > 0) SetPage(_currentPage - 1); }
	public int CurrentPage => _currentPage;
	public int TotalPages => _totalPages;

	private void UpdatePageLabel() {
		if (_pageLabel != null)
			_pageLabel.Text = $"Стр. {_currentPage + 1}/{_totalPages}";
	}

	public override bool ProcessKey(KeyEvent keyEvent) {
		switch (keyEvent.Key) {
			case Key.CursorUp:
				if (_selectedItem > 0) { _selectedItem--; EnsureVisible(); SetNeedsDisplay(); }
				return true;
			case Key.CursorDown:
				if (_selectedItem < _pageItems.Count - 1) { _selectedItem++; EnsureVisible(); SetNeedsDisplay(); }
				return true;
			case Key.Home:
				_selectedItem = 0;
				EnsureVisible();
				SetNeedsDisplay();
				return true;
			case Key.End:
				_selectedItem = Math.Max(0, _pageItems.Count - 1);
				EnsureVisible();
				SetNeedsDisplay();
				return true;
			case Key.PageUp:
				_selectedItem = Math.Max(0, _selectedItem - Bounds.Height);
				EnsureVisible();
				SetNeedsDisplay();
				return true;
			case Key.PageDown:
				_selectedItem = Math.Min(_pageItems.Count - 1, _selectedItem + Bounds.Height);
				EnsureVisible();
				SetNeedsDisplay();
				return true;
			case Key.CursorLeft:
				PrevPage();
				return true;
			case Key.CursorRight:
				NextPage();
				return true;
		}
		return base.ProcessKey(keyEvent);
	}

	private void EnsureVisible() {
		if (_selectedItem < _topItem)
			_topItem = _selectedItem;
		if (_selectedItem >= _topItem + Bounds.Height)
			_topItem = _selectedItem - Bounds.Height + 1;
	}

	public override void Redraw(Rect bounds) {
		var bg = TuiGame.BackgroundColor;
		var selBg = HasFocus ? Color.DarkGray : bg;

		for (int row = 0; row < bounds.Height; row++) {
			int idx = _topItem + row;
			bool isSel = idx == _selectedItem;
			Color rowBg = isSel ? selBg : bg;

			// Очистка
			Driver.SetAttribute(new Terminal.Gui.Attribute(Color.White, rowBg));
			Move(0, row);
			for (int x = 0; x < bounds.Width; x++)
				Driver.AddRune(' ');

			if (idx < 0 || idx >= _pageItems.Count)
				continue;

			var info = _pageItems[idx];
			int col = 0;

			// ── Входные предметы (полное описание каждого) ──
			var inputs = info.InputItems.Where(c => c.type is not null).ToArray();
			for (int i = 0; i < inputs.Length; i++) {
				if (i > 0)
					col = DrawText(" + ", col, row, Color.White, rowBg, bounds.Width);
				col = DrawFullCard(inputs[i], col, row, rowBg, bounds.Width);
			}

			// ── Стрелка результата ──
			string arrow = info.Result ? " ─✓→ " : " ─✗→ ";
			Color arrowColor = info.Result ? Color.BrightGreen : Color.BrightRed;
			col = DrawText(arrow, col, row, arrowColor, rowBg, bounds.Width);

			// ── Выходной предмет (полное описание) ──
			col = DrawFullCard(info.DropItem, col, row, rowBg, bounds.Width);

			// ── Шанс ──
			string chanceText = $" ({info.Chance:P2})";
			Color chanceColor = info.Chance switch {
				>= 0.45f => Color.BrightGreen,
				>= 0.20f => Color.BrightYellow,
				_ => Color.BrightRed
			};
			DrawText(chanceText, col, row, chanceColor, rowBg, bounds.Width);
		}
	}

	/// <summary>
	/// Рисует полное описание карты: "Id [Rarity] Price IWC"
	/// Всё окрашено в цвет редкости, хромики — с паттерном.
	/// </summary>
	private int DrawFullCard(CollectableCard card, int col, int y, Color rowBg, int maxW) {
		if (card.type is null)
			return col;

		string id = card.type.Id;
		string rarityLabel = UpgraderColors.RarityLabel(card.rarity);
		string priceStr = $"{card.price:F2}";
		var palette = UpgraderColors.GetColorSet(card.rarity);

		// Счётчик паттерна — единый для всей карточки
		int patternIdx = 0;

		// ID
		for (int i = 0; i < id.Length && col < maxW; i++, patternIdx++) {
			Driver.SetAttribute(new Terminal.Gui.Attribute(
				UpgraderColors.GetPattern(palette, patternIdx), rowBg));
			Move(col++, y);
			Driver.AddRune(id[i]);
		}

		// " ["
		col = DrawText(" [", col, y, Color.White, rowBg, maxW);

		// Rarity
		for (int i = 0; i < rarityLabel.Length && col < maxW; i++, patternIdx++) {
			Driver.SetAttribute(new Terminal.Gui.Attribute(
				UpgraderColors.GetPattern(palette, patternIdx), rowBg));
			Move(col++, y);
			Driver.AddRune(rarityLabel[i]);
		}

		// "] "
		col = DrawText("] ", col, y, Color.White, rowBg, maxW);

		// Price — цветом редкости
		for (int i = 0; i < priceStr.Length && col < maxW; i++, patternIdx++) {
			Driver.SetAttribute(new Terminal.Gui.Attribute(
				UpgraderColors.GetPattern(palette, patternIdx), rowBg));
			Move(col++, y);
			Driver.AddRune(priceStr[i]);
		}

		// " IWC" — белым
		col = DrawText($" {UpgraderGameView.Currency}", col, y, Color.White, rowBg, maxW);

		return col;
	}

	private int DrawText(string text, int col, int y, Color fg, Color bg, int maxW) {
		Driver.SetAttribute(new Terminal.Gui.Attribute(fg, bg));
		for (int i = 0; i < text.Length && col < maxW; i++) {
			Move(col++, y);
			Driver.AddRune(text[i]);
		}
		return col;
	}
}