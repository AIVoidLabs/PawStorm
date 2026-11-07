using System;
using Godot;

namespace PawStorm;

public partial class Hud : CanvasLayer
{
	[Export] public Label FishLabel { get; set; }
	[Export] public Label LivesLabel { get; set; }
	[Export] public Label SpeedLabel { get; set; }
	[Export] public Label TimeLabel { get; set; }
	[Export] public Label MessageLabel { get; set; }

	private PlayerController _player;

	public override void _Ready()
	{
		GameState.FishChanged += OnStateChanged;
		GameState.LivesChanged += OnStateChanged;
		GameState.LevelCompleted += OnLevelCompleted;
		_player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		MessageLabel.Visible = false;
		OnStateChanged(null, EventArgs.Empty);
	}

	public override void _ExitTree()
	{
		GameState.FishChanged -= OnStateChanged;
		GameState.LivesChanged -= OnStateChanged;
		GameState.LevelCompleted -= OnLevelCompleted;
	}

	private void OnStateChanged(object sender, EventArgs e)
	{
		FishLabel.Text = $"Fish: {GameState.Fish}";
		LivesLabel.Text = $"9 Lives: {GameState.Lives}";
	}

	private void OnLevelCompleted(object sender, EventArgs e)
	{
		var level = GetTree().GetFirstNodeInGroup("level_controller") as LevelController;
		string title = level?.LevelTitle ?? "Level";
		MessageLabel.Text = $"{title} TAMAM!\nStorm Core parçası: {GameState.CoreShards}/5\n"
			+ $"Balık: {GameState.Fish}   Süre: {FormatTime(GameState.LevelTime)}\n\n[Space] devam";
		MessageLabel.Visible = true;
	}

	private static string FormatTime(float t) => $"{(int)(t / 60):00}:{(int)(t % 60):00}";

	public override void _Process(double delta)
	{
		TimeLabel.Text = FormatTime(GameState.LevelTime);
		if (_player != null) SpeedLabel.Text = $"Speed: {_player.Speed * 3.6f:0} km/h";
	}
}
