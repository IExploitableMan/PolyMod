using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;

namespace PolyMod
{
	internal class DevConsole
	{
		private static bool _console = false;

		internal static void Init()
		{
			AddCommand("hack_stars", "[amount]", (args) =>
			{
				int amount = 100;
				if (args.Length > 0)
				{
					int.TryParse(args[0], out amount);
				}

				GameManager.LocalPlayer.Currency += amount;

				DebugConsole.Write($"+{amount} stars");
			});
			AddCommand("map_set", "(path)", (args) =>
			{
				if (args.Length == 0)
				{
					DebugConsole.Write("Wrong args!");
					return;
				}

				MapEditor.map = JObject.Parse(File.ReadAllText(args[0]));

				DebugConsole.Write($"Map set");
			});
			AddCommand("map_unset", "", (args) =>
			{
				MapEditor.map = null;

				DebugConsole.Write($"Map unset");
			});
			AddCommand("version_change", "Changes the version of the next singleplayer game", (args) =>
			{
				Plugin.version = int.Parse(args[0].ToString());

				DebugConsole.Write($"Next game will start with version {Plugin.version}");
			});
		}

		internal static void Toggle()
		{
			if (_console)
			{
				DebugConsole.Hide();
			}
			else
			{
				DebugConsole.Show();
			}
			_console = !_console;
		}
		private static void AddCommand(string name, string description, Action<Il2CppStringArray> container)
		{
			DebugConsole.AddCommand(name, DelegateSupport.ConvertDelegate<DebugConsole.CommandDelegate>(container), description);
		}
	}
}
