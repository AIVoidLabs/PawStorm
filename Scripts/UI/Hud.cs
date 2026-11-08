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
	private ChargeBar _chargeBar;

	public override void _Ready()
	{
		GameState.FishChanged += OnStateChanged;
		GameState.LivesChanged += OnStateChanged;
		GameState.LevelCompleted += OnLevelCompleted;
		GameState.GameOver += OnGameOver;
		_player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		MessageLabel.Visible = false;
		AddChild(new PauseMenu());

		_chargeBar = new ChargeBar { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
		_chargeBar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
		_chargeBar.OffsetLeft = -190;
		_chargeBar.OffsetRight = 190;
		_chargeBar.OffsetTop = -170;
		_chargeBar.OffsetBottom = -70;
		AddChild(_chargeBar);
		OnStateChanged(null, EventArgs.Empty);
	}

	public override void _ExitTree()
	{
		GameState.FishChanged -= OnStateChanged;
		GameState.LivesChanged -= OnStateChanged;
		GameState.LevelCompleted -= OnLevelCompleted;
		GameState.GameOver -= OnGameOver;
	}

	private void OnGameOver(object sender, EventArgs e)
	{
		if (_player == null || !IsInsideTree()) return;
		// the event fires from the player's physics step: build the overlay after it
		Callable.From(() => GameOverMenu.Show(this, _player)).CallDeferred();
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
		string path = GetTree().CurrentScene?.SceneFilePath ?? "";
		var info = Levels.For(path);
		bool final = info != null && Levels.Next(path) == Levels.MainMenu;
		string head;
		if (final) head = $"{title} yenildi!\nStorm Core birleşti, fırtına dindi ve kediler eve döndü.\nOYUN BİTTİ - Teşekkürler Pati!\n";
		else if (info?.Victory != null) head = info.Victory + "\n";
		else head = $"{title} TAMAM!\nStorm Core parçası: {GameState.CoreShards}/{Levels.ShardCount}\n";
		MessageLabel.Text = head
			+ $"Balık: {GameState.Fish}   Süre: {FormatTime(GameState.LevelTime)}\n\n[Space] devam";
		MessageLabel.Visible = true;
	}

	private static string FormatTime(float t) => $"{(int)(t / 60):00}:{(int)(t % 60):00}";

	public override void _Process(double delta)
	{
		if (_player != null) _chargeBar.Ratio = _player.ChargeRatio;
		TimeLabel.Text = FormatTime(GameState.LevelTime);
		if (GameState.BossMaxHp > 0)
			TimeLabel.Text += $"\n{GameState.BossName}: {new string('♥', Mathf.Max(GameState.BossHp, 0))}";
		if (_player != null) SpeedLabel.Text = $"Speed: {_player.Speed * 3.6f:0} km/h";
	}
}
