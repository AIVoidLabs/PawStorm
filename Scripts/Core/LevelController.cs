using Godot;

namespace PawStorm;

/// <summary>Per-level glue: timer, title, and what happens after the Storm Core is collected.</summary>
public partial class LevelController : Node
{
	[Export] public string LevelTitle { get; set; } = "Level";
	/// <summary>Scene loaded after completion; empty = back to the main menu.</summary>
	[Export(PropertyHint.File, "*.tscn")] public string NextScene { get; set; } = "";
	[Export] public float ContinueDelay { get; set; } = 1.5f;

	private float _doneTime;

	public override void _Ready()
	{
		AddToGroup("level_controller");
		GameState.BeginLevel();
		GetTree().Paused = false;
	}

	public override void _Process(double delta)
	{
		if (!GameState.LevelDone) { GameState.LevelTime += (float)delta; return; }
		_doneTime += (float)delta;
		if (_doneTime > ContinueDelay && (Input.IsActionJustPressed("jump") || Input.IsActionJustPressed("spin")))
		{
			string target = string.IsNullOrEmpty(NextScene) ? "res://Scenes/UI/MainMenu.tscn" : NextScene;
			GetTree().ChangeSceneToFile(target);
		}
	}
}
