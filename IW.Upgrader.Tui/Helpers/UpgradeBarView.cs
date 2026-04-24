using Terminal.Gui;

namespace IW.Upgrader.Tui.Helpers;

internal sealed class UpgradeBarView : View {
	private readonly int _barWidth;
	private readonly int _winStart;
	private readonly int _winEnd;

	public int ArrowPos { get; set; }

	public UpgradeBarView(int barWidth, int winStart, int winEnd) {
		_barWidth = barWidth;
		_winStart = winStart;
		_winEnd = winEnd;
		CanFocus = false;
		Width = barWidth;
		Height = 5;
	}

	public override void Redraw(Rect bounds) {
		var bg = TuiGame.BackgroundColor;

		// Очистка области
		for (int y = 0; y < 5; y++) {
			Driver.SetAttribute(new(Color.White, bg));
			Move(0, y);
			for (int x = 0; x < _barWidth; x++)
				Driver.AddRune(' ');
		}

		// Строка 0: Верхняя стрелка
		if (ArrowPos >= 0 && ArrowPos < _barWidth) {
			Driver.SetAttribute(new(Color.BrightYellow, bg));
			Move(ArrowPos, 0);
			Driver.AddRune('▼');
		}

		// Строка 1: Центральная шкала
		for (int i = 0; i < _barWidth; i++) {
			bool inWin = i >= _winStart && i <= _winEnd;
			bool isArrow = i == ArrowPos;

			Color cellColor = inWin ? Color.Green : Color.Red;

			Move(i, 1);

			if (isArrow) {
				Driver.SetAttribute(new(Color.BrightYellow, cellColor));
				Driver.AddRune('◆');
			} else {
				Driver.SetAttribute(new(cellColor, cellColor));
				Driver.AddRune('█');
			}
		}

		// Строка 2: Нижняя стрелка
		if (ArrowPos >= 0 && ArrowPos < _barWidth) {
			Driver.SetAttribute(new(Color.BrightYellow, bg));
			Move(ArrowPos, 2);
			Driver.AddRune('▲');
		}

		// Строка 3: Деления шкалы
		Driver.SetAttribute(new(Color.DarkGray, bg));
		var tickPositions = new HashSet<int>();
		for (int pct = 0; pct <= 100; pct += 10) {
			int pos = _barWidth > 1 ? (int)MathF.Round(pct / 100f * (_barWidth - 1)) : 0;
			tickPositions.Add(pos);
		}
		for (int i = 0; i < _barWidth; i++) {
			Move(i, 3);
			Driver.AddRune(tickPositions.Contains(i) ? '┬' : '─');
		}

		// Строка 4: Подписи процентов
		int step = _barWidth >= 60 ? 10 : _barWidth >= 30 ? 20 : 25;
		var labels = new List<(int pos, string text)>();
		for (int pct = 0; pct <= 100; pct += step) {
			int pos = _barWidth > 1 ? (int)MathF.Round(pct / 100f * (_barWidth - 1)) : 0;
			labels.Add((pos, $"{pct}%"));
		}

		int lastEnd = -2;
		foreach (var (pos, text) in labels) {
			int startX = pos - text.Length / 2;
			startX = Math.Max(0, Math.Min(startX, _barWidth - text.Length));

			// Исправление: < вместо <=
			if (startX < lastEnd + 1)
				continue;

			Driver.SetAttribute(new(Color.DarkGray, bg));
			Move(startX, 4);
			foreach (char ch in text)
				Driver.AddRune(ch);
			lastEnd = startX + text.Length - 1;
		}
	}
}