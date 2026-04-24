using DiscordRPC;
using IW.Upgrader.Card;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Terminal.Gui;

namespace IW.Upgrader.Tui;

public static class TuiGame {

	public const Color BackgroundColor = Color.Black;
	public const string VersionName = "1.4";
	public const string Name = $"IW Upgrader {VersionName}";
	public const string SaveFile = "save.iwupg";
	public const string ItemTypesFile = "item_types.json5";
	public const string ItemPacksFile = "item_packs.json5";
	public static readonly DiscordRpcClient DRpcClient = new("1497262733752533224");

	[DynamicDependency(DynamicallyAccessedMemberTypes.AllProperties | DynamicallyAccessedMemberTypes.AllConstructors, typeof(CollectableCardType))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.AllProperties | DynamicallyAccessedMemberTypes.AllConstructors, typeof(CollectableCardPack))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.AllProperties | DynamicallyAccessedMemberTypes.AllConstructors, typeof(CollectableCard.Selector))]
	public static void Main() {
		Console.OutputEncoding = Encoding.UTF8;
		Application.Init();
		ApplyTheme();
		try {
			DRpcClient.Initialize();
			DRpcClient.SetPresence(new() { Type = ActivityType.Playing , Assets = new() { LargeImageKey = "logo" } });
			Application.Run<MainMenuView>();
		} finally {
			Application.Shutdown();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ApplyTheme() {
		var bg = BackgroundColor;
		var fg = Color.White;
		var hotFg = Color.BrightYellow;

		var scheme = new ColorScheme {
			Normal = new(fg, bg),
			Focus = new(bg, fg),
			HotNormal = new(hotFg, bg),
			HotFocus = new(bg, hotFg),
			Disabled = new(Color.Gray, bg)
		};

		Colors.Base = scheme;
		Colors.Dialog = scheme;
		Colors.Menu = scheme;
		Colors.Error = new() {
			Normal = new(Color.BrightRed, bg),
			Focus = scheme.Focus,
			HotNormal = scheme.HotNormal,
			HotFocus = scheme.HotFocus,
			Disabled = scheme.Disabled
		};
	}
}