using System;

namespace PawStorm;

/// <summary>Level order of the campaign. The last level is followed by the main menu.</summary>
public static class Levels
{
	public const string MainMenu = "res://Scenes/UI/MainMenu.tscn";

	public static readonly string[] Order =
	{
		"res://Scenes/Levels/Level1_Rooftops.tscn",
		"res://Scenes/Levels/Level2_Harbor.tscn",
		"res://Scenes/Levels/Level3_Forest.tscn",
		"res://Scenes/Levels/Level4_Factory.tscn",
		"res://Scenes/Levels/Level5_SkyIsland.tscn",
		"res://Scenes/Levels/Level6_Boss.tscn",
	};

	public static string First => Order[0];

	public static string Next(string currentScenePath)
	{
		int i = Array.IndexOf(Order, currentScenePath);
		return i >= 0 && i + 1 < Order.Length ? Order[i + 1] : MainMenu;
	}
}
