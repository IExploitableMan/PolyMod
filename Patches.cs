using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using UnityEngine;

namespace PolyMod
{
	internal class Patches
	{

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameModeScreen), nameof(GameModeScreen.OnGameMode))]
		private static bool GameModeScreen_OnGameMode(GameMode gameMode)
		{
			if (!Plugin.bots_only || gameMode != GameMode.Custom)
			{
				return true;
			}
            GameSettingsExtensions.TryLoadFromDisk(out GameSettings gameSettings, GameType.SinglePlayer, gameMode);
            GameManager.PreliminaryGameSettings = gameSettings;
			GameManager.PreliminaryGameSettings.BaseGameMode = gameMode;
			GameManager.debugAutoPlayLocalPlayer = true;
			UIManager.Instance.ShowScreen(UIConstants.Screens.GameSetup, false);
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateCustomGameModeList))]
		static private bool GameSetupScreen_CreateCustomGameModeList(UIHorizontalList __result, GameSetupScreen __instance)
		{
			if (!Plugin.bots_only)
			{
				return true;
			}
			string[] array = new string[]
			{
				Localization.Get(GameModeUtils.GetTitle(GameMode.Perfection)),
				Localization.Get(GameModeUtils.GetTitle(GameMode.Domination))
				// Remove the Sandbox (infinite) mode
			};
			__result = __instance.CreateHorizontalList("gamesettings.mode", array, new Action<int>(__instance.OnCustomGameModeChanged), __instance.GetCustomGameModeIndexFromSettings(), null, -1, null);
			return false;
		}


		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateOpponentList))]
		private static bool GameSetupScreen_CreateOpponentList(ref UIHorizontalList __result, GameSetupScreen __instance, RectTransform parent = null)
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return true;
			}
			int maxOpponents = GameManager.GetMaxOpponents();
			int num = maxOpponents;
			if (GameManager.PreliminaryGameSettings.GameType == GameType.SinglePlayer)
			{
				num = Mathf.Min(GameManager.GetPurchaseManager().GetUnlockedTribeCount() - 1, maxOpponents);
			}
			string[] array = new string[maxOpponents + 1];
			for (int i = 0; i <= maxOpponents; i++)
			{
				array[i] = (i+1).ToString();
			}
			int num2 = Mathf.Min(num, GameManager.PreliminaryGameSettings.OpponentCount);
			__result = __instance.CreateHorizontalList("gamesettings.opponents", array, new Action<int>(__instance.OnOpponentsChanged), num2, parent, num + 1, new Action(__instance.OnTriedSelectDisabledOpponent));
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LocalClient), nameof(LocalClient.IsPlayerLocal))]
		public static bool LocalClient_IsPlayerLocal(ref bool __result, byte playerId)
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return true;
			}
			__result = true;
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.IsPlayerViewing))]
		public static bool GameManager_IsPlayerViewing(ref bool __result)
		{
			if (Plugin.unview)
			{
				__result = false;
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(WipePlayerReaction), nameof(WipePlayerReaction.Execute))]
		public static bool WipePlayerReaction_Execute()
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return true;
			}
			DebugConsole.Write($"GameManager.Client.ActionManager.isRecapping: {GameManager.Client.ActionManager.isRecapping}");
			GameManager.Client.ActionManager.isRecapping = true;
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(WipePlayerReaction), nameof(WipePlayerReaction.Execute))]
		public static void WipePlayerReaction_Execute_Postfix()
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return;
			}
			GameManager.Client.ActionManager.isRecapping = false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StartTurnReaction), nameof(StartTurnReaction.Execute))]
		public static bool StartTurnReaction_Execute()
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return true;
			}
			Plugin.localClient = GameManager.Client as LocalClient;
			if (Plugin.localClient == null)
			{
				DebugConsole.Write($"So GameManager.Client is {GameManager.Client.GetType()}? What?");
				return true;
			}
			// Replace the client (temporarily)
			GameManager.instance.client = new ReplayClient();
			GameManager.Client.currentGameState = Plugin.localClient.GameState;
			GameManager.Client.CreateOrResetActionManager(Plugin.localClient.lastSeenCommand);
			GameManager.Client.ActionManager.isRecapping = true;
			LevelManager.GetClientInteraction().DeselectUnit();
			LevelManager.GetClientInteraction().DeselectTile(); // Just in case the human was clicking on stuff
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapRenderer), nameof(MapRenderer.Refresh))]
		public static bool MapRenderer_Refresh()
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return true;
			}
			if (Plugin.localClient != null)
			{ // Repair the client as soon as possible
				GameManager.instance.client = Plugin.localClient;
				Plugin.localClient = null;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(TaskCompletedReaction), nameof(TaskCompletedReaction.Execute))]
		public static bool TaskCompletedReaction_Execute(ref TaskCompletedReaction __instance, ref byte __state)
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return true;
			}
			__state = __instance.action.PlayerId;
			__instance.action.PlayerId = 255;
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(TaskCompletedReaction), nameof(TaskCompletedReaction.Execute))]
		public static void TaskCompletedReaction_Execute_Postfix(ref TaskCompletedReaction __instance, ref byte __state)
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return;
			}
			__instance.action.PlayerId = __state;
		}

		// Patch multiple classes with the same method
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MeetReaction), nameof(MeetReaction.Execute))]
		[HarmonyPatch(typeof(EnableTaskReaction), nameof(EnableTaskReaction.Execute))]
		[HarmonyPatch(typeof(ExamineRuinsReaction), nameof(ExamineRuinsReaction.Execute))]
		[HarmonyPatch(typeof(InfiltrationRewardReaction), nameof(InfiltrationRewardReaction.Execute))]
		public static bool Patch_Execute()
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return true;
			}
			Plugin.unview = true;
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MeetReaction), nameof(MeetReaction.Execute))]
		[HarmonyPatch(typeof(EnableTaskReaction), nameof(EnableTaskReaction.Execute))]
		[HarmonyPatch(typeof(ExamineRuinsReaction), nameof(ExamineRuinsReaction.Execute))]
		[HarmonyPatch(typeof(InfiltrationRewardReaction), nameof(InfiltrationRewardReaction.Execute))]
		[HarmonyPatch(typeof(StartTurnReaction), nameof(StartTurnReaction.Execute))]
		public static void Patch_Execute_Post()
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				if (Plugin.unview)
				{
					DebugConsole.Write("Uh, what!?");
				}
				return;
			}
			Plugin.unview = false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ClientActionManager), nameof(ClientActionManager.Update))]
		public static void Patch_Update()
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				if (Plugin.unview)
				{
					DebugConsole.Write("Uh, what!?");
				}
				if (Plugin.localClient != null)
				{
					DebugConsole.Write("Sorry, what!?");
				}
				return;
			}
			if (Plugin.localClient != null)
			{
				GameManager.instance.client = Plugin.localClient;
				Plugin.localClient = null;
			}
			Plugin.unview = false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(LocalClient), nameof(LocalClient.CreateSession))]
		public static void LocalClient_CreateSession(ref Il2CppSystem.Threading.Tasks.Task<CreateSessionResult> __result, ref LocalClient __instance, GameSettings settings, List<PlayerState> players)
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return;
			}
			for (int j = 0; j < __instance.GameState.PlayerCount; j++)
			{
				PlayerState playerState = __instance.GameState.PlayerStates[j];
				playerState.AutoPlay = false;
				playerState.UserName = AccountManager.AliasInternal;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LocalClient), nameof(LocalClient.GetCurrentLocalPlayer))]
		public static bool LocalClient_GetCurrentLocalPlayer(ref PlayerState __result, ref LocalClient __instance)
		{
			MapEditor.PreGenerate(ref state, ref settings);
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		static void MapGenerator_Generate(ref GameState state, ref MapGeneratorSettings settings)
		{
			MapEditor.PreGenerate(ref state, ref settings);
		}


		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
		private static void GameManager_Update(GameManager __instance)
		{
			Plugin.Update();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SettingsScreen), nameof(SettingsScreen.CreateLanguageList))]
		private static bool SettingsScreen_CreateLanguageList(SettingsScreen __instance, UnityEngine.Transform parent)
		{
			List<string> list = new() { "Automatic", "English", "Français", "Deutsch", "Italiano", "Português", "Русский", "Español", "日本語", "한국어" };
			List<int> list2 = new() { 1, 3, 7, 9, 6, 4, 5, 8, 11, 12 };
			if (GameManager.GetPurchaseManager().IsTribeUnlocked(Polytopia.Data.TribeData.Type.Elyrion))
			{
				list.Add("∑∫ỹriȱŋ");
				list2.Add(10);
			}
			list.Add("Custom...");
			list2.Add(2);
			__instance.languageSelector = UnityEngine.Object.Instantiate(__instance.horizontalListPrefab, parent ?? __instance.container);
			__instance.languageSelector.UpdateScrollerOnHighlight = true;
			__instance.languageSelector.HeaderKey = "settings.language";
			__instance.languageSelector.SetIds(list2.ToArray());
			__instance.languageSelector.SetData(list.ToArray(), 0, false);
			__instance.languageSelector.SelectId(SettingsUtils.Language, true, -1f);
			__instance.languageSelector.IndexSelectedCallback = new Action<int>(__instance.LanguageChangedCallback);
			__instance.totalHeight += __instance.languageSelector.rectTransform.sizeDelta.y;

			return false;
		}
	}
}
