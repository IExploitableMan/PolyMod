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

		// [HarmonyPrefix]
		// [HarmonyPatch(typeof(EnableTaskReaction), nameof(EnableTaskReaction.Execute))]
		// public static bool EnableTaskReaction_PreExecute()
		// {
		// 	if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
		// 	{
		// 		Plugin.unview = true;
		// 	}
		// 	return false;
		// }

		// [HarmonyPostfix]
		// [HarmonyPatch(typeof(EnableTaskReaction), nameof(EnableTaskReaction.Execute))]
		// public static void EnableTaskReaction_PostExecute()
		// {
		// 	Plugin.unview = false;
		// }

		// [HarmonyPrefix]
		// [HarmonyPatch(typeof(TaskCompletedReaction), nameof(TaskCompletedReaction.Execute))]
		// public static bool TaskCompletedReaction_PreExecute()
		// {
		// 	if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
		// 	{
		// 		Plugin.unview = true;
		// 	}
		// 	return false;
		// }

		// [HarmonyPostfix]
		// [HarmonyPatch(typeof(TaskCompletedReaction), nameof(TaskCompletedReaction.Execute))]
		// public static void TaskCompletedReaction_PostExecute()
		// {
		// 	Plugin.unview = false;
		// }

		// [HarmonyPrefix]
		// [HarmonyPatch(typeof(MeetReaction), nameof(MeetReaction.Execute))]
		// public static bool MeetReaction_PreExecute()
		// {
		// 	if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
		// 	{
		// 		Plugin.unview = true;
		// 	}
		// 	return false;
		// }

		// [HarmonyPostfix]
		// [HarmonyPatch(typeof(MeetReaction), nameof(MeetReaction.Execute))]
		// public static void MeetReaction_PostExecute()
		// {
		// 	Plugin.unview = false;
		// }

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LocalClient), nameof(LocalClient.CreateSession))]
		public static bool CreateSession(ref Il2CppSystem.Threading.Tasks.Task<CreateSessionResult> __result, ref LocalClient __instance, GameSettings settings, List<PlayerState> players)
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return true;
			}
			__instance.ClearPreviousSession();
			__instance.Reset();
			__instance.gameId = Il2CppSystem.Guid.NewGuid();
			GameState gameState = new GameState
			{
				Version = VersionManager.GameVersion,
				Settings = settings,
				PlayerStates = new Il2CppSystem.Collections.Generic.List<PlayerState>()
			};
			if (settings.isWeeklyChallenge)
			{
				gameState.Seed = WeeklyChallenges.GetWeeklyChallengeSeed();
				for (int i = 0; i < settings.weeklyChallengeSettings.players.Count; i++)
				{
					PlayerData playerData = settings.weeklyChallengeSettings.players[i];
					bool flag = playerData.type == PlayerData.Type.Bot;
					gameState.PlayerStates.Add(new PlayerState
					{
						Id = (byte)(i + 1),
						AccountId = new Il2CppSystem.Nullable<Il2CppSystem.Guid>(Il2CppSystem.Guid.Empty),
						AutoPlay = flag,
						UserName = playerData.GetNameInternal(),
						tribe = playerData.tribe,
						tribeMix = playerData.tribeMix,
						skinType = playerData.skinType,
						hasChosenTribe = true
					});
				}
			}
			else
			{
				for (int j = 0; j < settings.OpponentCount + 1; j++)
				{
                    PlayerState playerState = new PlayerState
                    {
                        Id = (byte)(j + 1),
                        AccountId = new Il2CppSystem.Nullable<Il2CppSystem.Guid>(Il2CppSystem.Guid.Empty),
                        AutoPlay = false,
                        UserName = AccountManager.AliasInternal,
                        tribe = GameStateUtils.GetRandomPickableTribe(gameState),
                        tribeMix = TribeData.Type.None,
                        skinType = SkinType.Default,
                        hasChosenTribe = true,
                        handicap = GameSettings.HandicapFromDifficulty(settings.Difficulty)
                    };
                    gameState.PlayerStates.Add(playerState);
				}
			}
			GameStateUtils.SetPlayerColors(gameState);
			GameStateUtils.AddNaturePlayer(gameState);
			ushort num = (ushort)Math.Max(settings.MapSize, (int)MapDataExtensions.GetMinimumMapSize(gameState.PlayerCount));
			gameState.Map = new MapData(num, num);
			MapGeneratorSettings mapGeneratorSettings = settings.GetMapGeneratorSettings();
			MapGenerator mapGenerator = new MapGenerator();
			if (settings.isWeeklyChallenge)
			{
				mapGenerator.GenerateWithSeed(settings.weeklyChallengeSettings.seed, gameState, mapGeneratorSettings, null);
			}
			else
			{
				mapGenerator.Generate(gameState, mapGeneratorSettings, null);
			}
			foreach (PlayerState playerState2 in gameState.PlayerStates)
			{
				foreach (PlayerState playerState3 in gameState.PlayerStates)
				{
					playerState2.aggressions[playerState3.Id] = 0;
				}
				if (playerState2.Id != 255)
				{
					playerState2.Currency = 5;
                    if (gameState.GameLogicData.TryGetData(playerState2.tribe, out TribeData tribeData) && gameState.GameLogicData.TryGetData(tribeData.startingUnit.type, out UnitData unitData))
                    {
                        TileData tile = gameState.Map.GetTile(playerState2.startTile);
                        UnitState unitState = ActionUtils.TrainUnitScored(gameState, playerState2, tile, unitData);
                        unitState.attacked = false;
                        unitState.moved = false;
                    }
                }
			}
			SerializationHelpers.FromByteArray<GameState>(SerializationHelpers.ToByteArray<GameState>(gameState, gameState.Version), out gameState);
			__instance.initialGameState = gameState;
			gameState.CommandStack.Add(new StartMatchCommand(1));
			__instance.hasInitializedSaveData = true;
			__instance.UpdateGameStateImmediate(gameState, StateUpdateReason.GameCreated);
			__instance.SaveSession(__instance.gameId, false);
			__instance.PrepareSession();
			Il2CppSystem.Guid gameId = __instance.gameId;
			PlayerState currentLocalPlayer = __instance.GetCurrentLocalPlayer();
			AnalyticsHelpers.SendGameStartEvent(gameId, settings, (currentLocalPlayer != null) ? new Il2CppSystem.Nullable<TribeData.Type>(currentLocalPlayer.tribe) : null);
			__result = Il2CppSystem.Threading.Tasks.Task.FromResult<CreateSessionResult>(CreateSessionResult.Success);
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LocalClient), nameof(LocalClient.GetCurrentLocalPlayer))]
		public static bool LocalClient_GetCurrentLocalPlayer(ref PlayerState __result, ref LocalClient __instance)
		{
			if (!Plugin.bots_only || GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return true;
			}
			PlayerState playerState;
			if (__instance.GameState != null && __instance.GameState.TryGetPlayer((byte)(__instance.GameState.CurrentPlayerIndex + 1), out playerState))
			{
				__result = playerState;
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
		private static void GameManager_Update(GameManager __instance)
		{
			Plugin.Update();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.ExecuteCommand))]
		private static bool DebugConsole_ExecuteCommand(ref string command)
		{
			return !Commands.Execute(command);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.CmdHelp))]
		private static void DebugConsole_CmdHelp()
		{
			Commands.Help();
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
