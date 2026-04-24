// D:\Visual Studio Projects\IW.Upgrader\Tui\GameView.cs

using IW.Upgrader.Card;
using IW.Upgrader.Tui.Helpers;
using System.Globalization;
using Terminal.Gui;
using TextCopy;
using static IW.Upgrader.Tui.Helpers.CardListView;

namespace IW.Upgrader.Tui;

public sealed class UpgraderGameView : Toplevel {
	public const string Currency = "IWC";
	private readonly UpgraderGame _game;

	private readonly Label _lblBalance;
	private readonly CardLabel[] _slotLabels = new CardLabel[UpgraderGame.InputLimit];
	private readonly CardListView _invList, _catList;

	// Основные кнопки
	// В объявлении кнопок (строка ~20) добавь _btnSellAll:
	private readonly Button _btnBuy, _btnSell, _btnSellAll, _btnPut, _btnReset, _btnUpgrade,
							_btnHistory, _btnInfo, _btnNewGame, _btnExit;

	// Режим каталога
	private readonly Button _btnCatalogMode;
	private readonly FrameView _catFrame;
	private CatalogViewMode _catalogMode = CatalogViewMode.Cards;

	// Сортировка
	private readonly Button _btnSortMode, _btnSortDir;

	// Быстрый подбор дропа
	private readonly Button[] _quickBtns;
	private readonly Button _btnCustomize;
	private readonly QuickPickPreset[] _presets;

	private CatalogSortMode _sortMode = CatalogSortMode.Default;
	private bool _sortAscending = true;
	private bool _includeUnpurchasable = false;

	private enum CatalogSortMode { Default, ById, ByRarity, ByPrice }
	private static readonly string[] _sortModeLabels = ["—", "ID", "Редкость", "Цена"];

	private static readonly ColorScheme _disabledBtnScheme = new() {
		Normal = new(Color.DarkGray, TuiGame.BackgroundColor),
		Focus = new(Color.DarkGray, TuiGame.BackgroundColor),
		HotNormal = new(Color.DarkGray, TuiGame.BackgroundColor),
		HotFocus = new(Color.DarkGray, TuiGame.BackgroundColor),
		Disabled = new(Color.DarkGray, TuiGame.BackgroundColor),
	};

	// ═══════════════════════════════════════════
	//  Пресеты быстрого подбора
	// ═══════════════════════════════════════════

	private struct QuickPickPreset(float value) {
		public readonly string Label => IsMultiplier ? $"x{Value:F}" : $"{Value:P0}";
		public float Value { get; set; } = value;
		public readonly bool IsMultiplier => Value > 1f;
		public readonly override string ToString() => Label;
	}

	private static readonly QuickPickPreset[] QuickPickPresetDefault = [
		new(0.75f),
		new(0.50f),
		new(0.30f),
		new(2f),
		new(5f),
		new(10f),
	];

	// ═══════════════════════════════════════════
	//  Конструктор
	// ═══════════════════════════════════════════

	public UpgraderGameView(UpgraderGame game) {
		_game = game;
		_presets = [.. QuickPickPresetDefault];
		ColorScheme = Colors.Menu;

		var win = new Window(TuiGame.Name) { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
		Add(win);

		_btnUpgrade = new Button("") { X = Pos.Center(), Y = 0 };
		_btnUpgrade.Clicked += PerformUpgrade;
		// ── Верхняя панель ──
		_lblBalance = new Label("") { X = 1, Y = 0 };

		_btnSortDir = new Button("") { X = Pos.AnchorEnd(29), Y = 0 };
		_btnSortDir.Clicked += ToggleSortDirection;

		_btnSortMode = new Button("") { X = Pos.AnchorEnd(23), Y = 0 };
		_btnSortMode.Clicked += CycleSortMode;

		_btnCatalogMode = new("") { X = Pos.AnchorEnd(10), Y = 0 };
		_btnCatalogMode.Clicked += ToggleCatalogMode;


		// ── Слоты ──
		var slotsFrame = new FrameView("Слоты апгрейда") {
			X = 0,
			Y = 1,
			Width = Dim.Fill(),
			Height = UpgraderGame.InputLimit + 2
		};
		for (int i = 0; i < UpgraderGame.InputLimit; i++) {
			_slotLabels[i] = new CardLabel { X = 1, Y = i, Width = Dim.Fill() - 2, Height = 1 };
			slotsFrame.Add(_slotLabels[i]);
		}

		// ── Инвентарь ──
		int mainY = 1 + UpgraderGame.InputLimit + 2;
		var invFrame = new FrameView("Инвентарь") {
			X = 0,
			Y = mainY,
			Width = Dim.Percent(50),
			Height = Dim.Fill(6)
		};
		_invList = new CardListView { Width = Dim.Fill(), Height = Dim.Fill() };
		_invList.OpenSelectedItem += (_) => PutItem();
		_invList.SelectedItemChanged += (_) => UpdateButtons();
		invFrame.Add(_invList);

		// ── Каталог ──
		_catFrame = new FrameView("Каталог") {
			X = Pos.Percent(50),
			Y = mainY,
			Width = Dim.Fill(),
			Height = Dim.Fill(6)
		};
		_catList = new CardListView { Width = Dim.Fill(), Height = Dim.Fill() };
		_catList.OpenSelectedItem += (_) => OnCatalogActivated();
		_catList.SelectedItemChanged += (_) => UpdateButtons(); 
		_catFrame.Add(_catList);

		// ── Ряд 1: Быстрый подбор ──
		_quickBtns = new Button[_presets.Length];
		int qx = 1;
		for (int i = 0; i < _presets.Length; i++) {
			int idx = i;
			_quickBtns[i] = new Button(_presets[i].Label) { X = qx, Y = Pos.AnchorEnd(4) };
			_quickBtns[i].Clicked += () => QuickPick(idx);
			qx += _presets[i].Label.Length + 4;
		}

		_btnCustomize = new Button("⚙") { X = qx + 1, Y = Pos.AnchorEnd(4) };
		_btnCustomize.Clicked += CustomizePresets;


		int btnR = 2;
		_btnBuy = Btn("Купить", 1, btnR, OnBuyClicked);
		_btnSell = Btn("Продать", 11, btnR, SellItem);
		_btnSellAll = Btn("Продать всё", 22, btnR, SellAll);
		_btnPut = Btn("В слот", 37, btnR, PutItem);
		_btnReset = Btn("Вернуть", 47, btnR, ResetSlots);
		_btnHistory = Btn("История", 58, btnR, ShowHistory);
		_btnInfo = Btn("Статистика", 69, btnR, ShowGameInfo);
		_btnNewGame = Btn("Новая игра", 83, btnR, () => {
			if (!ShowSaveDialog())
				return;
			MainMenuView.StartNewGame();
		});
		_btnExit = Btn("Выход", 97, btnR, () => {
			if (!ShowSaveDialog())
				return;
			Application.RequestStop();
		});

		win.Add(
			_lblBalance, _btnUpgrade, _btnSortMode, _btnSortDir, _btnCatalogMode,
			slotsFrame, invFrame, _catFrame,
			_btnBuy, _btnSell, _btnSellAll, _btnPut, _btnReset,
			_btnHistory, _btnInfo, _btnNewGame, _btnExit,
			_btnCustomize
		);
		foreach (var qb in _quickBtns) win.Add(qb);
		TuiGame.DRpcClient.UpdateStartTime();
		TuiGame.DRpcClient.UpdateState($"In the game (Ver: {TuiGame.VersionName})");
		TuiGame.DRpcClient.UpdateDetails($"Balance: {_game.Balance:F2} {Currency}");
		Refresh();
	}

	private void SellAll() {
		if (!_btnSellAll.Enabled)
			return;

		var inventory = _game.Inventory;
		if (inventory.Count == 0) {
			Dialogs.Info("Продажа", "Инвентарь пуст!");
			return;
		}

		var bg = TuiGame.BackgroundColor;
		var dlg = new Dialog("Продать всё", 60, 10);

		// Галочка "Включая непокупаемые"
		int unpurchaseableCount = inventory.Count(c => !c.IsPurchaseable);
		var cbInclude = new CheckBox($"Продать непокупаемые? (Пропущенно: {unpurchaseableCount})", _includeUnpurchasable) {
			X = Pos.Center(),
			Y = 1
		};

		// Лейбл с суммой, обновляется при переключении галочки
		var lblSummary = new Label("") {
			X = Pos.Center(),
			Y = 3,

			Height = 2,
			ColorScheme = UpgraderColors.MakeScheme(Color.BrightCyan, bg)
		};

		void UpdateSummary(bool checkedBox) {
			_includeUnpurchasable = checkedBox;
			var toSell = checkedBox
				? inventory
				: inventory.Where(c => c.IsPurchaseable);

			lblSummary.Text = $"Кол-во: {toSell.Count()} \nСумма: {toSell.Sum(c => c.price):F2} {Currency}";
			lblSummary.SetNeedsDisplay();
		}

		cbInclude.Toggled += UpdateSummary;
		UpdateSummary(cbInclude.Checked);

		dlg.Add(cbInclude, lblSummary);

		bool confirmed = false;

		var btnConfirm = new Button("Продать");
		btnConfirm.Clicked += () => {
			confirmed = true;
			Application.RequestStop(dlg);
		};

		var btnCancel = new Button("Отмена");
		btnCancel.Clicked += () => Application.RequestStop(dlg);

		dlg.AddButton(btnConfirm);
		dlg.AddButton(btnCancel);

		Application.Run(dlg);

		if (!confirmed) return;

		bool includeUnpurchaseable = cbInclude.Checked;

		for (int i = inventory.Count - 1; i >= 0; i--) {
			if (!includeUnpurchaseable && !inventory[i].IsPurchaseable)
				continue;
			_game.Sell(i);
		}

		Refresh();
	}

	private bool ShowSaveDialog() {
		var bg = TuiGame.BackgroundColor;

		var dlg = new Dialog("Сохранение", 50, 8);

		dlg.Add(new Label("Сохранить текущую игру?") {
			X = Pos.Center(),
			Y = 1,
			ColorScheme = UpgraderColors.MakeScheme(Color.White, bg)
		});
		bool result = false;
		var btnSave = new Button("Сохранить");
		btnSave.Clicked += () => {
			File.WriteAllBytes(TuiGame.SaveFile, _game.Save());
			Application.RequestStop(dlg);
			result = true;
		};

		var btnNoSave = new Button("Не сохранять");
		btnNoSave.Clicked += () => {
			Application.RequestStop(dlg);
			result = true;
		};

		var btnCancel = new Button("Отмена");
		btnCancel.Clicked += () => {
			Application.RequestStop(dlg);
		};

		dlg.AddButton(btnSave);
		dlg.AddButton(btnNoSave);
		dlg.AddButton(btnCancel);

		Application.Run(dlg);
		return result;
	}

	private static Button Btn(string t, int x, int y, Action a) {
		var b = new Button(t) { X = x, Y = Pos.AnchorEnd(y) };
		b.Clicked += a;
		return b;
	}

	// ═══════════════════════════════════════════
	//  Переключение режима каталога
	// ═══════════════════════════════════════════

	private void ToggleCatalogMode() {
		var enumValues = Enum.GetValues<CatalogViewMode>();
		_catalogMode = enumValues[(int)(_catalogMode + 1) % enumValues.Length];
		RedrawCatalogBtn();
	}

	// ═══════════════════════════════════════════
	//  Сортировка паков
	// ═══════════════════════════════════════════

	private IEnumerable<CollectableCardPack> GetSortedPacks() {
		var packs = _game.CardPacks;
		return _sortMode switch {
			CatalogSortMode.ById => _sortAscending
				? packs.OrderBy(p => p.Id)
				: packs.OrderByDescending(p => p.Id),
			CatalogSortMode.ByPrice => _sortAscending
				? packs.OrderBy(p => p.Price)
				: packs.OrderByDescending(p => p.Price),
			_ => packs
		};
	}

	private void RedrawCatalogBtn() {
		switch (_catalogMode) {
			case CatalogViewMode.Cards:
				_btnCatalogMode.Text = "Карты";
				_catList.SetCards(GetSortedCards());
				break;

			case CatalogViewMode.Packs:
				_btnCatalogMode.Text = "Паки";
				_catList.SetPacks(GetSortedPacks(), _game.Balance);
				break;
		}
		UpdateButtons();
	}

	private void OnCatalogActivated() {
		if (_catalogMode == CatalogViewMode.Cards)
			BuyItem();
		else
			BuyPack();
	}

	private void OnBuyClicked() {
		if (_catalogMode == CatalogViewMode.Cards)
			BuyItem();
		else
			BuyPack();
	}

	// ═══════════════════════════════════════════
	//  Покупка пака
	// ═══════════════════════════════════════════

	private void BuyPack() {
		int idx = _catList.SelectedItem;
		var packs = _game.CardPacks;
		if (idx < 0 || idx >= packs.Count)
			return;

		var pack = packs[idx];

		if (_game.Balance < pack.Price) {
			Dialogs.Info("Магазин",
				$"Не хватает {pack.Price - _game.Balance:F2} {Currency}!");
			return;
		}

		if (!Dialogs.Confirm("Подтверждение",
			$"Купить \"{pack.Id}\" за {pack.Price:F2} {Currency}?"))
			return;

		if (!_game.BuyPack(idx, out var droppedCards)) {
			Dialogs.Info("Ошибка", "Не удалось купить пак.");
			return;
		}

		var cards = droppedCards.ToArray();
		if (cards.Length == 0) {
			Dialogs.Info("Пусто", "Пак оказался пустым.");
			Refresh();
			return;
		}

		ShowRevealDialog(pack, cards);
		Refresh();
	}

	private static void ShowRevealDialog(CollectableCardPack pack, CollectableCard[] cards) {
		var bg = TuiGame.BackgroundColor;
		int totalCards = cards.Length;
		bool[] revealed = new bool[totalCards];
		int revealedCount = 0;

		// ── Размеры ──
		int dlgWidth = Math.Max(70, 10);
		int cardAreaHeight = totalCards * cards.Length;
		int dlgHeight = cardAreaHeight + 7;

		var dlg = new Dialog($"Пак: {pack.Id}", dlgWidth, dlgHeight);

		// ── Карточки ──
		var cardFrames = new FrameView[totalCards];
		var cardLabels = new CardLabel[totalCards];
		var revealBtns = new Button[totalCards];

		var dlgButton = new Button("Открыть все");
		dlgButton.Clicked += () => {
			for (int i = 0; i < totalCards; i++) {
				RevealCard(i);
			}
		};

		// Функция открытия карты
		void RevealCard(int idx) {
			if (revealed[idx])
				return;

			revealed[idx] = true;
			revealedCount++;

			var card = cards[idx];
			string text = UpgraderColors.CardText(card);
			cardLabels[idx].SetColored("  ", text, card.rarity);

			cardFrames[idx].Title = $"Карта {idx + 1}:";
			revealBtns[idx].Visible = false;

			if (revealedCount >= totalCards) {
				float totalPrice = cards.Sum(c => c.price);
				dlg.Add(
					new Label($"Стоимость дропа: {totalPrice:F2} {Currency} (x{totalPrice / pack.Price:F2})") {
						X = Pos.Center(),
						Y = Pos.AnchorEnd(cards.Length),
						ColorScheme = UpgraderColors.MakeScheme(Color.BrightCyan, bg)
					}
				);

				dlgButton.Text = "Закрыть";
				dlgButton.Clicked += () => Application.RequestStop(dlg);
			}

			dlg.SetNeedsDisplay();
		}

		dlg.AddButton(dlgButton);


		for (int i = 0; i < totalCards; i++) {
			int idx = i;
			int yPos = 1 + i * cards.Length;

			cardFrames[i] = new FrameView($"Карта {i + 1}") {
				X = 1,
				Y = yPos,
				Width = Dim.Fill() - 2,
				Height = cards.Length
			};

			cardLabels[i] = new CardLabel {
				X = 0,
				Y = 0,
				Width = Dim.Fill(),
				Height = 1
			};
			cardLabels[i].SetEmpty("  ??? — Нажмите «Открыть»");

			revealBtns[i] = new Button("Открыть") {
				X = Pos.AnchorEnd(13),
				Y = 0
			};
			revealBtns[i].Clicked += () => RevealCard(idx);

			cardFrames[i].Add(cardLabels[i], revealBtns[i]);
			dlg.Add(cardFrames[i]);
		}

		Application.Run(dlg);
	}

	// ═══════════════════════════════════════════
	//  Сортировка
	// ═══════════════════════════════════════════

	private void CycleSortMode() {
		_sortMode = (CatalogSortMode)(((int)_sortMode + 1) % _sortModeLabels.Length);
		UpdateSortButtons();
		RefreshCatalog();
	}

	private void ToggleSortDirection() {
		_sortAscending = !_sortAscending;
		UpdateSortButtons();
		RefreshCatalog();
	}

	private void UpdateSortButtons() {
		var bg = TuiGame.BackgroundColor;

		_btnSortMode.Text = _sortModeLabels[(int)_sortMode];
		_btnSortMode.ColorScheme = _sortMode != CatalogSortMode.Default
			? UpgraderColors.MakeScheme(Color.BrightCyan, bg) : Colors.Base;

		_btnSortDir.Text = _sortAscending ? "↑" : "↓";
		_btnSortDir.ColorScheme = _sortMode != CatalogSortMode.Default
			? UpgraderColors.MakeScheme(Color.BrightCyan, bg) : Colors.Base;

		SetBtnState(_btnSortDir, _sortMode != CatalogSortMode.Default);
	}

	private void RefreshCatalog() {
		_invList.SetCards(GetSortedInventory());
		if (_catalogMode == CatalogViewMode.Cards)
			_catList.SetCards(GetSortedCards());
		else
			_catList.SetPacks(GetSortedPacks(), _game.Balance);
		UpdateButtons();
	}

	// ═══════════════════════════════════════════
	//  Быстрый подбор дропа
	// ═══════════════════════════════════════════

	private void QuickPick(int presetIdx) {
		if (presetIdx < 0 || presetIdx >= _presets.Length)
			return;

		float inputSum = _game.UpgradeInput.Sum(c => c.price);
		if (inputSum <= 0) {
			Dialogs.Info("Подбор", "Сначала положите предметы в слоты!");
			return;
		}

		var preset = _presets[presetIdx];
		float targetPrice = preset.IsMultiplier
			? inputSum * preset.Value
			: inputSum / preset.Value;

		var catalog = GetSortedCards().ToList();
		if (catalog.Count == 0)
			return;

		int bestIdx = 0;
		float bestDiff = float.MaxValue;
		for (int i = 0; i < catalog.Count; i++) {
			float diff = MathF.Abs(catalog[i].price - targetPrice);
			if (diff < bestDiff) { bestDiff = diff; bestIdx = i; }
		}

		_catList.SelectedItem = bestIdx;
		_catList.SetNeedsDisplay();
		UpdateButtons();
	}

	private void CustomizePresets() {
		var bg = TuiGame.BackgroundColor;
		var dlg = new Dialog("Настройка быстрых кнопок", 55, _presets.Length * 3 + 8);

		var valueFields = new TextField[_presets.Length];
		var isMultLabels = new Label[_presets.Length];

		for (int i = 0; i < _presets.Length; i++) {
			int idx = i;
			int y = i * 3;

			dlg.Add(new Label($"Кнопка {i + 1}:") {
				X = 1,
				Y = y,
				ColorScheme = UpgraderColors.MakeScheme(Color.BrightCyan, bg)
			});

			var preset = _presets[i];
			var isMultLabel = new Label(preset.IsMultiplier ? "(множитель)" : "(шанс 0-1)") {
				X = 15,
				Y = y + 1,
				Width = 15,
				ColorScheme = UpgraderColors.MakeScheme(Color.DarkGray, bg)
			};
			isMultLabels[i] = isMultLabel;
			dlg.Add(isMultLabel);

			var valueField = new TextField(preset.Value.ToString("F2", CultureInfo.InvariantCulture)) {
				X = 1,
				Y = y + 1,
				Width = 10
			};
			valueField.TextChanged += _ => {
				string text = valueField.Text?.ToString() ?? "";
				if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) && val > 0) {
					bool isMult = val > 1f;
					isMultLabel.Text = isMult ? "(множитель)" : "(шанс 0-1)";
					isMultLabel.ColorScheme = UpgraderColors.MakeScheme(Color.DarkGray, bg);
				} else {
					isMultLabel.Text = "(неверно)";
					isMultLabel.ColorScheme = UpgraderColors.MakeScheme(Color.BrightRed, bg);
				}
				isMultLabel.SetNeedsDisplay();
			};

			dlg.Add(valueField);
			valueFields[i] = valueField;
		}

		var btnSave = new Button("Сохранить");
		btnSave.Clicked += () => {
			for (int i = 0; i < _presets.Length; i++) {
				if (!float.TryParse(valueFields[i].Text?.ToString(),
					NumberStyles.Float,
					CultureInfo.InvariantCulture,
					out float val) || val <= 0) {
					Dialogs.Info("Ошибка", $"Неверное значение в кнопке {i + 1}");
					return;
				}
				_presets[i].Value = val;
			}
			RebuildQuickButtons();
			Application.RequestStop(dlg);
		};

		var btnReset = new Button("По умолчанию");
		btnReset.Clicked += () => {
			for (int i = 0; i < _presets.Length; i++) {
				valueFields[i].Text = QuickPickPresetDefault[i].Value.ToString("F2", CultureInfo.InvariantCulture);
			}
		};

		var btnCancel = new Button("Отмена");
		btnCancel.Clicked += () => Application.RequestStop(dlg);

		dlg.AddButton(btnSave);
		dlg.AddButton(btnReset);
		dlg.AddButton(btnCancel);
		Application.Run(dlg);
	}

	private void RebuildQuickButtons() {
		for (int i = 0; i < _quickBtns.Length && i < _presets.Length; i++) _quickBtns[i].Text = _presets[i].Label;
		UpdateButtons();
	}

	private static void SetBtnState(Button btn, bool enabled) {
		btn.Enabled = enabled;
		btn.CanFocus = enabled;
		btn.ColorScheme = enabled ? Colors.Base : _disabledBtnScheme;
	}

	private void UpdateButtons() {
		var selInv = _invList.SelectedCard;
		bool hasInv = selInv.type != null;
		bool hasSlots = _game.UpgradeInput.Any(c => c.type != null);
		bool hasFreeSlot = _game.UpgradeInput.Any(c => c.type == null);
		bool hasHistory = _game.UpgradeHistory.Count > 0;
		bool isCards = _catalogMode == CatalogViewMode.Cards;

		if (_catalogMode == CatalogViewMode.Cards) {
			var selCat = _catList.SelectedCard;
			bool hasCat = selCat.type != null;
			bool canAfford = hasCat && _game.Balance >= selCat.price;
			float ch = hasCat ? _game.GetDropChance(selCat) : 0;

			_btnUpgrade.Text = "★ Апгрейд ★";
			SetBtnState(_btnBuy, canAfford);
			SetBtnState(_btnUpgrade, hasSlots && hasCat && UpgraderGame.ValidChance(ch));
			foreach (var qb in _quickBtns)
				SetBtnState(qb, hasSlots);
		} else {
			var packs = _game.CardPacks;
			int idx = _catList.SelectedItem;
			bool validPack = idx >= 0 && idx < packs.Count;
			bool canAffordPack = validPack && _game.Balance >= packs[idx].Price;

			_btnUpgrade.Text = "⌸ Дроп пака ⌸";
			SetBtnState(_btnBuy, canAffordPack);
			SetBtnState(_btnUpgrade, validPack);
			foreach (var qb in _quickBtns)
				SetBtnState(qb, false);
		}
		SetBtnState(_btnSellAll, _game.Inventory.Count > 0);
		SetBtnState(_btnSell, hasInv);
		SetBtnState(_btnPut, hasInv && hasFreeSlot);
		SetBtnState(_btnReset, hasSlots);
		SetBtnState(_btnHistory, hasHistory);

		bool canSort = true;
		SetBtnState(_btnSortMode, canSort);
		SetBtnState(_btnSortDir, canSort && _sortMode != CatalogSortMode.Default);
		SetBtnState(_btnCustomize, isCards);
	}

	private void Refresh() {
		TuiGame.DRpcClient.UpdateDetails($"Balance: {_game.Balance:F2} {Currency}");

		_lblBalance.Text = $"Баланс: {_game.Balance:F2} {Currency}";
		_lblBalance.ColorScheme =
			UpgraderColors.MakeScheme(Color.BrightYellow, TuiGame.BackgroundColor);

		for (int i = 0; i < UpgraderGame.InputLimit; i++) {
			var c = _game.UpgradeInput[i];
			if (c.type != null)
				_slotLabels[i].SetColored($"Слот {i + 1}: ",
					UpgraderColors.CardText(c), c.rarity);
			else
				_slotLabels[i].SetEmpty($"Слот {i + 1}: [пусто]");
		}

		_invList.SetCards(GetSortedInventory());
		RedrawCatalogBtn();
		UpdateSortButtons();
	}

	// ═══════════════════════════════════════════
	//  Действия
	// ═══════════════════════════════════════════

	private IEnumerable<CollectableCard> GetSortedInventory() {
		var inv = _game.Inventory;
		return _sortMode switch {
			CatalogSortMode.ById => _sortAscending
				? inv.OrderBy(p => p.type.Id)
				: inv.OrderByDescending(p => p.type.Id),
			CatalogSortMode.ByRarity => _sortAscending
				? inv.OrderBy(c => c.rarity).ThenBy(c => c.type.Id)
				: inv.OrderByDescending(c => c.rarity).ThenBy(c => c.type.Id),
			CatalogSortMode.ByPrice => _sortAscending
				? inv.OrderBy(p => p.price)
				: inv.OrderByDescending(p => p.price),
			_ => inv
		};
	}

	private void BuyItem() {
		if (!_btnBuy.Enabled || _catalogMode != CatalogViewMode.Cards)
			return;
		var sel = _catList.SelectedCard;
		if (sel.type is null)
			return;
		if (_game.Balance < sel.price) {
			Dialogs.Info("Магазин",
				$"Не хватает {sel.price - _game.Balance:F2} {Currency}");
			return;
		}
		_game.BuyCard(FindCatalogIdx(sel));
		Refresh();
	}

	private void SellItem() {
		if (!_btnSell.Enabled)
			return;
		var sel = _invList.SelectedCard;
		if (sel.type is null)
			return;
		_game.Sell(FindInvIdx(sel));
		Refresh();
	}

	private void PutItem() {
		if (!_btnPut.Enabled)
			return;
		var sel = _invList.SelectedCard;
		if (sel.type is null)
			return;
		if (_game.UpgradeInput.All(c => c.type != null)) {
			Dialogs.Info("Слоты", "Нет свободных слотов!");
			return;
		}
		_game.Put(FindInvIdx(sel));
		Refresh();
	}

	private void ResetSlots() {
		if (!_btnReset.Enabled)
			return;
		_game.UndoPut();
		Refresh();
	}

	// Добавь новый метод ShowPackDropInfo рядом с BuyPack:

	private void ShowPackDropInfo() {
		int idx = _catList.SelectedItem;
		var packs = _game.CardPacks;
		if (idx < 0 || idx >= packs.Count)
			return;

		var pack = packs[idx];
		var bg = TuiGame.BackgroundColor;

		// Собираем все возможные дропы из пака, сортируем по цене (убывание)
		var drops = pack.CardSet
			.OrderByDescending(c => c.price)
			.ToList();

		if (drops.Count == 0) {
			Dialogs.Info("Дроп", "У этого пака нет возможных дропов.");
			return;
		}

		int dlgHeight = Math.Min(drops.Count + 8, 30);
		var dlg = new Dialog($"Дроп", 80, dlgHeight);

		// Заголовок: цена пака и кол-во дропов
		dlg.Add(new Label($"Цена пака \"{pack.Id}\": {pack.Price:F2} {Currency}\nПредметов: {drops.Count}") {
			X = 1,
			Y = 0,
			ColorScheme = UpgraderColors.MakeScheme(Color.BrightYellow, bg)
		});

		var sep = new LineView(Terminal.Gui.Graphs.Orientation.Horizontal) {
			X = 0,
			Y = 2,
			Width = Dim.Fill()
		};
		dlg.Add(sep);

		// Список дропов
		var dropList = new CardListView {
			X = 1,
			Y = 3,
			Width = Dim.Fill() - 2,
			Height = Dim.Fill() - 3
		};
		dropList.SetCards(drops);
		dlg.Add(dropList);

		var btnClose = new Button("Закрыть") { IsDefault = true };
		btnClose.Clicked += () => Application.RequestStop(dlg);
		dlg.AddButton(btnClose);

		Application.Run(dlg);
	}

	private void PerformUpgrade() {
		if (!_btnUpgrade.Enabled)
			return;

		// Режим паков — показать возможный дроп
		if (_catalogMode == CatalogViewMode.Packs) {
			ShowPackDropInfo();
			return;
		}

		// Режим карт — обычный апгрейд
		var target = _catList.SelectedCard;
		if (target.type is null)
			return;
		float ch = _game.GetDropChance(target);
		if (!UpgraderGame.ValidChance(ch))
			return;

		UpgradeWheelDialog.Show(target, ch, () => _game.Upgrade(FindCatalogIdx(target)));
		Refresh();
	}

	// ═══════════════════════════════════════════
	//  История
	// ═══════════════════════════════════════════

	// ═══════════════════════════════════════════
	//  Информация об игре
	// ═══════════════════════════════════════════

	private void ShowGameInfo() {
		var bg = TuiGame.BackgroundColor;

		var dlg = new Dialog("Информация об игре", 50, 11);

		dlg.Add(new Label("Изначальный сид:") {
			X = 2,
			Y = 1,
			ColorScheme = UpgraderColors.MakeScheme(Color.BrightCyan, bg)
		});
		dlg.Add(new Label(_game.SourceSeed.ToString()) {
			X = Pos.Percent(50),
			Y = 1,
			ColorScheme = UpgraderColors.MakeScheme(Color.BrightYellow, bg)
		});

		dlg.Add(new Label("Время игры:") {
			X = 2,
			Y = 3,
			ColorScheme = UpgraderColors.MakeScheme(Color.BrightCyan, bg)
		});

		var lblElapsed = new Label("") {
			X = Pos.Percent(50),
			Y = 3,
			Width = 30,
			ColorScheme = UpgraderColors.MakeScheme(Color.White, bg)
		};
		dlg.Add(lblElapsed);

		dlg.Add(new Label("Время начала игры:") {
			X = 2,
			Y = 5,
			ColorScheme = UpgraderColors.MakeScheme(Color.BrightCyan, bg)
		});
		dlg.Add(new Label(_game.StartTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")) {
			X = Pos.Percent(50),
			Y = 5,
			ColorScheme = UpgraderColors.MakeScheme(Color.DarkGray, bg)
		});

		// Форматирование elapsed time
		static string FormatElapsed(TimeSpan ts) =>
			ts.TotalHours >= 1
				? $"{(int)ts.TotalHours}ч {ts.Minutes:D2}м {ts.Seconds:D2}с"
				: ts.TotalMinutes >= 1
					? $"{ts.Minutes}м {ts.Seconds:D2}с"
					: $"{ts.Seconds}с";

		lblElapsed.Text = FormatElapsed(_game.ElapsedTime);

		object token = null!;
		token = Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(1), _ => {
			lblElapsed.Text = FormatElapsed(_game.ElapsedTime);
			dlg.SetNeedsDisplay();
			return true;
		});

		var btnCopySeed = new Button("Скопировать сид");
		btnCopySeed.Clicked += () => {
			ClipboardService.SetText(_game.SourceSeed.ToString());
			btnCopySeed.Text = "Скопирован!";
		};
		dlg.AddButton(btnCopySeed);
		var btnClose = new Button("Закрыть") { IsDefault = true };
		btnClose.Clicked += () => {
			Application.MainLoop.RemoveTimeout(token);
			Application.RequestStop(dlg);
		};
		dlg.AddButton(btnClose);
		
		Application.Run(dlg);
	}

	private void ShowHistory() {
		if (!_btnHistory.Enabled)
			return;

		var history = _game.UpgradeHistory.Reverse().ToList();
		if (history.Count == 0) {
			Dialogs.Info("История", "Нет апгрейдов.");
			return;
		}

		var bg = TuiGame.BackgroundColor;
		var dlg = new Dialog("История апгрейдов", 90, 30);

		// ── Статистика ──
		int total = history.Count;
		float winrate = total > 0 ? (float)history.Count(i => i.Result) / total : 0;

		var statsLabel = new Label(
			$"Всего апгрейдов: {total} | Винрейт: {winrate:P1}") {
			X = Pos.Center(),
			Y = 0,
			Width = Dim.Fill() - 2,
			Height = 1,
			TextAlignment = TextAlignment.Centered,
			ColorScheme = UpgraderColors.MakeScheme(Color.Cyan, bg)
		};

		var sep1 = new LineView(Terminal.Gui.Graphs.Orientation.Horizontal) {
			X = 0,
			Y = 1,
			Width = Dim.Fill()
		};

		// ═══════════════════════════════════════════
		//  Универсальный рендер блока апгрейда
		// ═══════════════════════════════════════════

		FrameView RenderUpgradeBlock(UpgradeInfo info, string title, Color frameColor) {
			int inputCount = info.InputItems.Count(c => c.type is not null);
			int blockHeight = inputCount + 8;

			var frame = new FrameView(title) {
				X = Pos.Center(),
				Y = 0,
				Width = Dim.Fill() - 4,
				Height = blockHeight,
				ColorScheme = UpgraderColors.MakeScheme(frameColor, bg)
			};

			int row = 0;

			// ── Шанс ──
			frame.Add(new Label($"Шанс: {info.Chance:P2}") {
				X = 1,
				Y = row,
				ColorScheme = UpgraderColors.MakeScheme(
					info.Chance >= 0.5f ? Color.BrightGreen :
					info.Chance >= 0.2f ? Color.BrightYellow :
					Color.BrightRed, bg)
			});

			// ── Множитель ──
			float inputSum = info.InputItems.Sum(c => c.price);
			float multiplier = inputSum > 0 ? info.DropItem.price / inputSum : 0;
			frame.Add(new Label($"x{multiplier:F2}") {
				X = Pos.AnchorEnd(10),
				Y = row,
				ColorScheme = UpgraderColors.MakeScheme(Color.BrightCyan, bg)
			});
			row++;

			frame.Add(new LineView(Terminal.Gui.Graphs.Orientation.Horizontal) {
				X = 0,
				Y = row,
				Width = Dim.Fill()
			});
			row++;

			// ── Вход ──
			frame.Add(new Label($"Лот ({inputSum:F2} {Currency}):") {
				X = 1,
				Y = row,
				ColorScheme = UpgraderColors.MakeScheme(Color.DarkGray, bg)
			});
			row++;

			foreach (var card in info.InputItems) {
				if (card.type is null)
					continue;
				var cl = new CardLabel { X = Pos.Center(), Y = row, Width = Dim.Fill() - 4, Height = 1 };
				cl.SetColored("", $"{card.type.Id} [{UpgraderColors.RarityLabel(card.rarity)}] {card.price:F2} {Currency}", card.rarity);
				frame.Add(cl);
				row++;
			}

			frame.Add(new LineView(Terminal.Gui.Graphs.Orientation.Horizontal) {
				X = 0,
				Y = row,
				Width = Dim.Fill()
			});
			row++;

			
			frame.Add(new Label($"Цель ({info.DropItem.price:F2} {Currency}):") {
				X = 1,
				Y = row,
				ColorScheme = UpgraderColors.MakeScheme(Color.DarkGray, bg)
			});
			
			row++;

			var dropLabel = new CardLabel { X = Pos.Center(), Y = row, Width = Dim.Fill() - 4, Height = 1 };
			var drop = info.DropItem;
			dropLabel.SetColored("", $"{drop.type.Id} [{UpgraderColors.RarityLabel(drop.rarity)}] {drop.price:F2} {Currency}", drop.rarity);
			frame.Add(dropLabel);

			return frame;
		}

		// ── Лучший дроп ──
		var bestInfo = _game.BestUpgrade;
		int bestBlockHeight = 0;
		FrameView bestFrame = null!;

		if (bestInfo is not null) {
			bestFrame = RenderUpgradeBlock(bestInfo, "★ Лучший дроп", Color.BrightYellow);
			bestFrame.Y = 2;
			bestBlockHeight = bestFrame.Frame.Height;
		}

		// ── Разделитель ──
		int historyStartY = bestInfo is not null ? 2 + bestBlockHeight + 1 : 2;

		var sep2 = new LineView(Terminal.Gui.Graphs.Orientation.Horizontal) {
			X = 0,
			Y = historyStartY - 1,
			Width = Dim.Fill()
		};

		// ── Пагинация ──
		int currentPage = 0;
		int totalPages = history.Count;

		var pageLabel = new Label("") {
			X = Pos.Center(),
			Y = historyStartY,
			Width = 30,
			TextAlignment = TextAlignment.Centered,
			ColorScheme = UpgraderColors.MakeScheme(Color.White, bg)
		};

		var container = new View {
			X = 0,
			Y = historyStartY + 1,
			Width = Dim.Fill(),
			Height = Dim.Fill() - 3
		};

		void RenderPage() {
			container.RemoveAll();

			if (currentPage < 0 || currentPage >= history.Count)
				return;

			var entry = history[currentPage];
			int entryNumber = currentPage + 1;

			string resultSymbol = entry.Result ? "✓ ПОБЕДА" : "✗ ПРОИГРЫШ";
			Color resultColor = entry.Result ? Color.BrightGreen : Color.BrightRed;

			var frame = RenderUpgradeBlock(entry, $"#{entryNumber} — {resultSymbol}", resultColor);
			container.Add(frame);

			pageLabel.Text = $"Апгрейд {currentPage + 1} / {totalPages}";
			container.SetNeedsDisplay();
			dlg.SetNeedsDisplay();
		}

		// ── Кнопки ──
		var btnPrev = new Button("◄ Пред.") { Enabled = currentPage > 0 };
		btnPrev.Clicked += () => {
			if (currentPage > 0) { currentPage--; RenderPage(); }

		};

		var btnNext = new Button("След. ►") { Enabled = currentPage < totalPages - 1 };
		btnNext.Clicked += () => {
			if (currentPage < totalPages - 1) { currentPage++; RenderPage(); }
		};

		var btnClose = new Button("Закрыть");
		btnClose.Clicked += () => Application.RequestStop(dlg);

		dlg.Add(statsLabel, sep1);
		if (bestInfo is not null)
			dlg.Add(bestFrame);
		dlg.Add(sep2, pageLabel, container);
		dlg.AddButton(btnPrev);
		dlg.AddButton(btnNext);
		dlg.AddButton(btnClose);

		RenderPage();
		Application.Run(dlg);
	}

	private IEnumerable<CollectableCard> GetSortedCards() {
		var cat = _game.Catalog.Where(i => i.IsPurchaseable);
		var src = cat.All(c => _game.GetDropChance(c) == 0)
			? cat : cat.Where(c => UpgraderGame.ValidChance(_game.GetDropChance(c)));
		return _sortMode switch {
			CatalogSortMode.ById => _sortAscending
				? src.OrderBy(c => c.type.Id)
				: src.OrderByDescending(c => c.type.Id),
			CatalogSortMode.ByRarity => _sortAscending
				? src.OrderBy(c => c.rarity).ThenBy(c => c.type.Id)
				: src.OrderByDescending(c => c.rarity).ThenBy(c => c.type.Id),
			CatalogSortMode.ByPrice => _sortAscending
				? src.OrderBy(c => c.price)
				: src.OrderByDescending(c => c.price),
			_ => src
		};
	}

	// ═══════════════════════════════════════════
	//  Поиск
	// ═══════════════════════════════════════════

	private int FindCatalogIdx(CollectableCard c) {
		for (int i = 0; i < _game.Catalog.Count; i++)
			if (_game.Catalog[i].type?.Id == c.type?.Id
				&& _game.Catalog[i].rarity == c.rarity)
				return i;
		return -1;
	}

	private int FindInvIdx(CollectableCard c) {
		for (int i = 0; i < _game.Inventory.Count; i++)
			if (_game.Inventory[i].type?.Id == c.type?.Id
				&& _game.Inventory[i].rarity == c.rarity)
				return i;
		return -1;
	}
}