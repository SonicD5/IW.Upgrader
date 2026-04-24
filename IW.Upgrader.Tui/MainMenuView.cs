using Terminal.Gui;
using IW.Upgrader.Tui.Helpers;
using SonicD5.Json;
using IW.Upgrader.Card;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace IW.Upgrader.Tui;

public sealed class MainMenuView : Toplevel {
	public MainMenuView() {
		ColorScheme = Colors.Base;
		Window win = new() {
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Fill(),
		};
		Add(win);

		var label = new Label(TuiGame.Name) {
			X = Pos.Center(),
			Y = Pos.Percent(20) + 3,
		};

		var btnContinue = new Button("Продолжить") { X = Pos.Center(), Y = Pos.Percent(20) + 6, Enabled = File.Exists(TuiGame.SaveFile)};
		var btnNew = new Button("Новая игра") { X = Pos.Center(), Y = Pos.Percent(20) + 8 };
		var btnQuit = new Button("Выйти") { X = Pos.Center(), Y = Pos.Percent(20) + 10 };

		btnContinue.Clicked += () => {
			if (!TryReadJson<CollectableCardType>(TuiGame.ItemTypesFile, out var types) ||
			!TryReadJson<CollectableCardPack>(TuiGame.ItemPacksFile, out var packs))
				return;
			try {
				Application.Run(new UpgraderGameView(new(types, packs, File.ReadAllBytes(TuiGame.SaveFile))));
			} catch (ArgumentException) {
				Dialogs.Info("Ошибка", $"Неподдерживаемый формат сохранения");
			} catch (Exception ex) {
				Dialogs.Info("Ошибка", $"Не удалось загрузить сохранение:\n{ex}");
			}
		};
		btnNew.Clicked += StartNewGame;
		btnQuit.Clicked += () => Application.RequestStop();

		win.Add(label, btnContinue, btnNew, btnQuit);
		Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(1), _ =>
		{
			bool canContinue = File.Exists(TuiGame.SaveFile);

			if (btnContinue.Enabled != canContinue) {
				btnContinue.Enabled = canContinue;
				btnContinue.SetNeedsDisplay();
			}
			return true;
		});
		TuiGame.DRpcClient.UpdateStartTime();
		TuiGame.DRpcClient.UpdateState("In main menu");
		TuiGame.DRpcClient.UpdateDetails($"Version: {TuiGame.VersionName}");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryReadJson<T>(string fileName, [MaybeNullWhen(false)] out List<T> result) {
		if (!JsonSerializer.TryDeserialize(File.ReadAllText(fileName), new() { CodecPack = [UpgraderJsonCodecs.CollectableCardPackCodec] }, out result)) {
			Dialogs.Info("Ошибка", $"Не удалось загрузить {fileName}");
			return false;
		} 
		if (result is null || result.Count == 0) {
			Dialogs.Info("Ошибка", $"Файл {fileName} пуст или имеет неверный формат.");
			return false;
		}
		return true;
	}

	public static void StartNewGame() {
		string? seedText = null;

		var seedDlg = new Dialog("Настройка игры", 50, 10);
		var seedLabel = new Label("Сид (необъязательно):") { X = 1, Y = 1 };
		var seedField = new TextField("") { X = 1, Y = 2, Width = 40 };


		var btnOk = new Button("Старт") { IsDefault = true };
		var btnCan = new Button("Отмена");
		btnOk.Clicked += () => { seedText = seedField.Text.ToString(); Application.RequestStop(seedDlg); };
		btnCan.Clicked += () => Application.RequestStop(seedDlg);

		seedDlg.AddButton(btnOk);
		seedDlg.AddButton(btnCan);
		seedDlg.Add(seedLabel, seedField);
		Application.Run(seedDlg);

		if (seedText is null) return; // пользователь нажал Отмена

		

		if (!TryReadJson<CollectableCardType>(TuiGame.ItemTypesFile, out var types) || 
			!TryReadJson<CollectableCardPack>(TuiGame.ItemPacksFile, out var packs)) return;

#if DEBUG
		float balance = float.PositiveInfinity;
#else
		float balance = 67f;
#endif
		Application.Run(new UpgraderGameView(string.IsNullOrWhiteSpace(seedText)
			? new(types, packs, balance)
			: int.TryParse(seedText, out int s)
				? new(types, packs, balance, s)
				: new(types, packs, balance, seedText.GetHashCode())));
	}
}