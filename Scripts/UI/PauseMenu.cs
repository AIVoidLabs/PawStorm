using Godot;

namespace PawStorm;

/// <summary>Pause overlay (Esc / Start). Created by the HUD; keeps running while the tree is paused.</summary>
public partial class PauseMenu : CanvasLayer
{
	private Control _root;

	public override void _Ready()
	{
		Layer = 10;
		ProcessMode = ProcessModeEnum.Always;
		_root = new Control { Visible = false };
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_root);

		var dim = new ColorRect { Color = new Color(0, 0, 0, 0.6f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(dim);

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		box.AddThemeConstantOverride("separation", 16);
		_root.AddChild(box);

		var title = new Label { Text = "Duraklatıldı", HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", 56);
		box.AddChild(title);
		box.AddChild(MakeButton("Devam", () => SetPaused(false)));
		box.AddChild(MakeButton("Ana Menü", () => GetTree().ChangeSceneToFile(Levels.MainMenu)));
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (e.IsActionPressed("pause")) { SetPaused(!GetTree().Paused); GetViewport().SetInputAsHandled(); }
	}

	private void SetPaused(bool paused)
	{
		GetTree().Paused = paused;
		_root.Visible = paused;
		Input.MouseMode = paused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
	}

	private static Button MakeButton(string text, System.Action onPressed)
	{
		var b = new Button { Text = text, CustomMinimumSize = new Vector2(260, 52), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
		b.AddThemeFontSizeOverride("font_size", 26);
		b.Pressed += onPressed;
		return b;
	}
}
