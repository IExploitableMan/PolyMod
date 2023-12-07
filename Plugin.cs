using BepInEx;
using HarmonyLib;
using PolytopiaBackendBase.Game;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace PolyMod
{
	[BepInPlugin("com.polymod", "PolyMod", "1.0.0.0")]
	public class Plugin : BepInEx.Unity.IL2CPP.BasePlugin
	{
		internal const uint MAP_MIN_SIZE = 6;
		internal const uint MAP_MAX_SIZE = 100;

		internal static bool console = false;
		internal static bool skip_recap = false;

		internal static bool view_current = false;

		internal static bool bots_only = true;

		internal static bool unview = false;

		public override void Load()
		{
			Harmony.CreateAndPatchAll(typeof(Patches));

			Commands.Add("bots_only", string.Empty, (args) =>
			{
				bots_only = !bots_only;
				DebugConsole.Write($"Bots only is {bots_only}");
			});

			Commands.Add("starhack", "[amount]", (args) =>
			{
				int amount = 0;
				if (args.Length > 0)
				{
					int.TryParse(args[0], out amount);
				}
				GameManager.LocalPlayer.Currency += amount;
				DebugConsole.Write($"+{amount} stars");
			});
			Commands.Add("setmap", "<path>", (args) =>
			{
				if (args.Length == 0)
				{
					DebugConsole.Write("Wrong args!");
				}
				MapEditor.mapPath = args[0];
				DebugConsole.Write($"Map set");
			});
			Commands.Add("unsetmap", "", (args) =>
			{
				MapEditor.mapPath = string.Empty;
				DebugConsole.Write($"Map unset");
			});
		}

		internal static void Update()
		{
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab))
			{
				if (console)
				{
					DebugConsole.Hide();
				}
				else
				{
					DebugConsole.Show();
				}
				console = !console;
			}
		}

		internal static Il2CppReferenceArray<T> WrapArray<T>(params T[] array) where T : Il2CppObjectBase
		{
			return new Il2CppReferenceArray<T>(array);
		}
	}
}
