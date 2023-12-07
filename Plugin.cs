using BepInEx;
using HarmonyLib;
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

		internal enum PatchedGameMode
		{
			None, // Use the original GameMode
			Bot
		}

		public class PatchedGamemodeButton : GamemodeButton
		{
			
		}

		internal static PatchedGameMode gameMode = PatchedGameMode.None;

		public override void Load()
		{
			Harmony.CreateAndPatchAll(typeof(Patches));

			Commands.Add("view_current", string.Empty, (args) =>
			{
				view_current = !view_current;
				DebugConsole.Write($"View current player is {view_current}");
			});

			Commands.Add("auto_play", string.Empty, (args) =>
			{
				GameManager.debugAutoPlayLocalPlayer = !GameManager.debugAutoPlayLocalPlayer;
				DebugConsole.Write($"Auto play is {GameManager.debugAutoPlayLocalPlayer}");
			});

			Commands.Add("skip_recap", string.Empty, (args) =>
			{
				skip_recap = !skip_recap;
				DebugConsole.Write($"Skip recap is {skip_recap}");
			});

			Commands.Add("unset_AutoPlay", string.Empty, (args) =>
			{
				foreach (var player in GameManager.GameState.PlayerStates)
				{
					player.AutoPlay = false;
				}
				DebugConsole.Write($"Auto play is unset");
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
