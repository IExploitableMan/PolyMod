using Newtonsoft.Json.Linq;
using Polytopia.Data;

namespace PolyMod
{
	internal static class MapEditor
	{
		internal static JObject? customMap;

		internal static void PreGenerate(ref GameState state, ref MapGeneratorSettings settings)
		{
			if (customMap == null)
			{
				return;
			}

			ushort size = (ushort)customMap["size"];
			if (size < Plugin.MAP_MIN_SIZE || size > Plugin.MAP_MAX_SIZE)
			{
				throw new Exception($"The map size must be between {Plugin.MAP_MIN_SIZE} and {Plugin.MAP_MAX_SIZE}");
			}
			state.Map = new(size, size);
			settings.mapType = PolytopiaBackendBase.Game.MapPreset.Dryland;
		}

		internal static void PostGenerate(ref GameState state)
		{
			if (customMap == null)
			{
				return;
			}
			MapData map = state.Map;

			for (int i = 0; i < map.tiles.Length; i++)
			{
				TileData tile = map.tiles[i];
				JToken tileJson = customMap["map"][i];

				if (tileJson["skip"] != null && (bool)tileJson["skip"]) continue;
				if (tile.rulingCityCoordinates == WorldCoordinates.NULL_COORDINATES)
				{
					tile.resource = tileJson["resource"] == null ? null : new() { type = EnumCache<ResourceData.Type>.GetType((string)tileJson["resource"]) };
				}
				if (tile.rulingCityCoordinates != tile.coordinates)
				{
					tile.improvement = tileJson["improvement"] == null ? null : new() { type = EnumCache<ImprovementData.Type>.GetType((string)tileJson["improvement"]) };
					if (tile.improvement != null && tile.improvement.type == ImprovementData.Type.City)
					{
						tile.improvement = new ImprovementState
						{
							type = ImprovementData.Type.City,
							founded = 0,
							level = 1,
							borderSize = 1,
							production = 1
						};
					}
				}

				tile.climate = (tileJson["climate"] == null || (int)tileJson["climate"] < 0 || (int)tileJson["climate"] > 16) ? 0 : (int)tileJson["climate"];
				tile.skinType = tileJson["skinType"] == null ? SkinType.Default : EnumCache<SkinType>.GetType((string)tileJson["skinType"]);
				tile.terrain = tileJson["terrain"] == null ? TerrainData.Type.None : EnumCache<TerrainData.Type>.GetType((string)tileJson["terrain"]);

				switch (tile.terrain)
				{
					case TerrainData.Type.Water:
						tile.altitude = -1;
						break;
					case TerrainData.Type.Ocean:
					case TerrainData.Type.Ice:
						tile.altitude = -2;
						break;
					case TerrainData.Type.Field:
					case TerrainData.Type.Forest:
						tile.altitude = 1;
						break;
					case TerrainData.Type.Mountain:
						tile.altitude = 2;
						break;
				}

				map.tiles[i] = tile;
			}

			customMap = null;
		}
	}
}
