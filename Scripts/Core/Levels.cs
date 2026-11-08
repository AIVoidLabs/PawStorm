using System;

namespace PawStorm;

/// <summary>Level order of the campaign plus per-level title, music and ambience. The last level is followed by the main menu.</summary>
public static class Levels
{
	public const string MainMenu = "res://Scenes/UI/MainMenu.tscn";

	public sealed record Info(string Path, string Title, string Music, string Ambient, string Preview);

	public static readonly Info[] All =
	{
		new("res://Scenes/Levels/Level1_Rooftops.tscn", "Çatılar Şehri", "rooftops", "amb_rooftops", "rooftops"),
		new("res://Scenes/Levels/Level2_Harbor.tscn", "Liman", "harbor", "amb_harbor", "harbor"),
		new("res://Scenes/Levels/Level3_Forest.tscn", "Orman", "forest", "amb_forest", "forest"),
		new("res://Scenes/Levels/Level4_Factory.tscn", "Fabrika", "factory", "amb_factory", "factory"),
		new("res://Scenes/Levels/Level5_SkyIsland.tscn", "Gökyüzü Adası", "sky", "amb_sky", "sky"),
		new("res://Scenes/Levels/Level6_Boss.tscn", "Dr. Kennel", "boss", "amb_boss", "boss"),
	};

	public static string First => All[0].Path;

	public static Info For(string scenePath) => Array.Find(All, l => l.Path == scenePath);

	public static string Next(string currentScenePath)
	{
		int i = Array.FindIndex(All, l => l.Path == currentScenePath);
		return i >= 0 && i + 1 < All.Length ? All[i + 1].Path : MainMenu;
	}
}
