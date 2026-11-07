using System;
using Godot;

namespace PawStorm;

public partial class Hud : CanvasLayer
{
	[Export] public Label FishLabel { get; set; }
	[Export] public Label SpeedLabel { get; set; }

	private PlayerController _player;

	public override void _Ready()
	{
		GameState.FishChanged += OnFishChanged;
		_player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		OnFishChanged(null, EventArgs.Empty);
	}

	public override void _ExitTree() => GameState.FishChanged -= OnFishChanged;

	private void OnFishChanged(object sender, EventArgs e) => FishLabel.Text = $"Fish: {GameState.Fish}";

	public override void _Process(double delta)
	{
		if (_player == null) return;
		SpeedLabel.Text = $"Speed: {_player.Speed * 3.6f:0} km/h";
	}
}
