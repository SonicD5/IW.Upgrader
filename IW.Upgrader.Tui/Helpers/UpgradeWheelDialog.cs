using IW.Upgrader.Card;
using Terminal.Gui;

namespace IW.Upgrader.Tui.Helpers;

public static class UpgradeWheelDialog {
	private const int BarWidth = 60;
	private static bool _lastFastChoice = false;

	public static void Show(CollectableCard target, float chance, Func<bool> upgrade) {
		var bg = TuiGame.BackgroundColor;

		bool isFast = _lastFastChoice;

		var speedDlg = new Dialog("Апгрейд", 55, 10);

		var cbFast = new CheckBox("Быстрая прокрутка", _lastFastChoice) {
			X = Pos.Center(),
			Y = 1
		};

		string cardPrefix = "Цель: ";
		string cardInfo = $"{target.type.Id} [{UpgraderColors.RarityLabel(target.rarity)}] {target.price:F2} {UpgraderGameView.Currency}";
		int cardTextLen = cardPrefix.Length + cardInfo.Length;

		var cardLbl = new CardLabel() {
			X = Pos.Center(),
			Y = 5,
			Width = cardTextLen,
			Height = 1
		};
		cardLbl.SetColored(cardPrefix, cardInfo, target.rarity);

		speedDlg.Add(
			cbFast,
			new Label($"Шанс: {chance:P2}") {
				X = Pos.Center(),
				Y = 3,
				ColorScheme = UpgraderColors.MakeScheme(
					chance >= 0.45f ? Color.BrightGreen :
					chance >= 0.20f ? Color.BrightYellow : Color.BrightRed, bg)
			},
			cardLbl
		);

		var btnStart = new Button("Крутить") { IsDefault = true };
		btnStart.Clicked += () => {
			isFast = cbFast.Checked;
			_lastFastChoice = isFast;
			Application.RequestStop(speedDlg);
		};
		bool cancelled = false;
		var btnCancel = new Button("Отмена");
		btnCancel.Clicked += () => {
			cancelled = true;
			Application.RequestStop(speedDlg);
		};

		speedDlg.AddButton(btnStart);
		speedDlg.AddButton(btnCancel);
		Application.Run(speedDlg);

		if (cancelled) return;

		var dlg = new Dialog("Апгрейд", BarWidth + 14, 14);

		int winCells = Math.Max(1, (int)MathF.Round(BarWidth * chance));
		int winStart = 0;
		int winEnd = winCells - 1;

		var rng = new Random();
		bool success = upgrade();
		int finalPos = success
			? rng.Next(winStart, winEnd + 1)
			: rng.Next(winEnd + 1, BarWidth);

		var chanceLabel = new Label($"Шанс: {chance:P2}") {
			X = Pos.Center(),
			Y = 1,
			ColorScheme = UpgraderColors.MakeScheme(
				chance >= 0.45f ? Color.BrightGreen :
				chance >= 0.20f ? Color.BrightYellow : Color.BrightRed, bg)
		};

		var barView = new UpgradeBarView(BarWidth, winStart, winEnd) {
			X = 4,
			Y = 2,
			Width = BarWidth,
			Height = 5
		};

		var resultLabel = new Label("") {
			X = Pos.Center(),
			Y = 8,
			Width = Dim.Fill() - 2,
			TextAlignment = TextAlignment.Centered
		};

		bool animDone = false;
		dlg.Add(chanceLabel, barView, resultLabel);

		// ── Показать финальный результат ──
		void ShowResult() {
			if (animDone)
				return;

			barView.ArrowPos = finalPos;
			barView.SetNeedsDisplay();
			animDone = true;

			var btnClose = new Button("Закрыть");
			btnClose.Clicked += () => Application.RequestStop(dlg);
			dlg.AddButton(btnClose);

			if (success) {
				resultLabel.Text = "УСПЕХ! Предмет получен!";
				resultLabel.ColorScheme = UpgraderColors.MakeScheme(Color.BrightGreen, bg);
			} else {
				resultLabel.Text = "ПРОВАЛ. Предметы сгорели.";
				resultLabel.ColorScheme = UpgraderColors.MakeScheme(Color.BrightRed, bg);
			}
			dlg.SetNeedsDisplay();
		}

		// ── Анимация ──
		int totalSteps = isFast ? 35 : 90;
		int baseDelay = isFast ? 10 : 25;
		int loops = isFast ? 2 : 4;
		int totalTravel = loops * BarWidth + finalPos;
		int currentStep = 0;

		void RunAnimation() {
			if (animDone || currentStep >= totalSteps) {
				ShowResult();
				return;
			}

			float t = (float)currentStep / totalSteps;
			float eased = 1f - MathF.Pow(1f - t, 4);
			barView.ArrowPos = (int)(totalTravel * eased) % BarWidth;
			barView.SetNeedsDisplay();

			int delay = (int)(baseDelay * (1f + t * t * 20f));
			Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(delay), (_) => {
				currentStep++;
				RunAnimation();
				return false;
			});
		}

		RunAnimation();
		Application.Run(dlg);
	}
}