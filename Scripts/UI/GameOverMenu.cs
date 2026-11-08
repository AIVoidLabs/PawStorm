using Godot;

namespace PawStorm;

/// <summary>"Out of lives" overlay. Pauses the game and offers: last checkpoint / restart level / main menu.</summary>
public partial class GameOverMenu : CanvasLayer
{
	private PlayerController _player;
	private Button _first;

	public static GameOverMenu Show(Node parent, PlayerController player)
	{
		var menu = new GameOverMenu { _player = player };
		parent.AddChild(menu);
		return menu;
	}

	public override void _Ready()
	{
		Layer = 20;
		ProcessMode = ProcessModeEnum.Always;
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Music.Duck(true);

		var root = new Control { ProcessMode = ProcessModeEnum.Always };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);
		var dim = new ColorRect { Color = new Color(0.12f, 0.02f, 0.05f, 0.72f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(dim);

		var panel = UiStyle.MakePanel(40);
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		panel.GrowHorizontal = Control.GrowDirection.Both;
		panel.GrowVertical = Control.GrowDirection.Both;
		root.AddChild(panel);

		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 16);
		panel.AddChild(v);
		v.AddChild(UiStyle.MakeLabel("Canların bitti!", 56, new Color(1f, 0.4f, 0.35f), outline: true));
		v.AddChild(UiStyle.MakeLabel("Pati dinlenip yeniden denemek istiyor.", 22));

		_first = UiStyle.MakeButton("Son Checkpoint'ten Devam", () => Close(() =>
		{
			GameState.Continue();
			_player.ReviveAtCheckpoint();
		}), 28, new Vector2(420, 60));
		v.AddChild(_first);
		v.AddChild(UiStyle.MakeButton("Bölümü Baştan Oyna", () => Close(() =>
		{
			GameState.Continue();
			GameState.BeginLevel();
			GetTree().ReloadCurrentScene();
		}), 28, new Vector2(420, 60)));
		v.AddChild(UiStyle.MakeButton("Ana Menü", () => Close(() => GetTree().ChangeSceneToFile(Levels.MainMenu)), 28, new Vector2(420, 60)));
		_first.GrabFocus();
	}

	private void Close(System.Action then)
	{
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		Music.Duck(false);
		then();
		QueueFree();
	}
}
