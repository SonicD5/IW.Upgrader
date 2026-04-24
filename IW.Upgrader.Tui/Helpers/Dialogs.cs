using Terminal.Gui;

namespace IW.Upgrader.Tui.Helpers;

public static class Dialogs {
	public static void Info(string title, string message) => MessageBox.Query(title, message, "OK");

	public static bool Confirm(string title, string message) => MessageBox.Query(title, message, "Да", "Нет") == 0;

	/// <summary>Показывает список и возвращает выбранный индекс или -1.</summary>
	public static int PickFromList(
		string title,
		IReadOnlyList<string> items,
		Func<int, ColorScheme?>? colorizer = null) {

		int chosen = -1;

		var dlg = new Dialog(title, 70, Math.Min(items.Count + 8, 30));

		var list = new ListView(items.ToList()) {
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 2,
			Height = Dim.Fill() - 4,
			AllowsMarking = false,
			ColorScheme = Colors.Dialog
		};

		if (colorizer != null) {
			list.RowRender += (args) => {
				var scheme = colorizer(args.Row);
				if (scheme != null)
					args.RowAttribute = scheme.Normal;
			};
		}

		var btnOk = new Button("OK") { IsDefault = true };
		btnOk.Clicked += () => {
			chosen = list.SelectedItem;
			Application.RequestStop(dlg);
		};

		var btnCancel = new Button("Отмена");
		btnCancel.Clicked += () => Application.RequestStop(dlg);

		dlg.AddButton(btnOk);
		dlg.AddButton(btnCancel);
		dlg.Add(list);

		Application.Run(dlg);
		return chosen;
	}
}