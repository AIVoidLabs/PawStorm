using Godot;

namespace PawStorm;

/// <summary>Title screen, built in code so the scene stays a single node.</summary>
public partial class MainMenu : Control
{
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().Paused = false;
		SetAnchorsPreset(LayoutPreset.FullRect);

		var bg = new ColorRect { Color = new Color(0.25f, 0.5f, 0.9f) };
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.SetAnchorsPreset(LayoutPreset.FullRect);
		box.AddThemeConstantOverride("separation", 16);
		AddChild(box);

		box.AddChild(MakeLabel("PawStorm", 96));
		box.AddChild(MakeLabel("Pati ve Storm Core", 28));

		var play = MakeButton("Oyna");
		play.Pressed += () =>
		{
			GameState.ResetAll();
			GetTree().ChangeSceneToFile(Levels.First);
		};
		box.AddChild(play);

		var quit = MakeButton("Çıkış");
		quit.Pressed += () => GetTree().Quit();
		box.AddChild(quit);

		box.AddChild(MakeLabel(
			"WASD / sol analog: hareket    Space / A: zıpla (havada: Pounce, Air Twist)\n"
			+ "Shift / X: Pati Sprint (basılı tut, bırak)    Fare / sağ analog: kamera    Esc: duraklat", 18));

		play.GrabFocus();
	}

	private static Label MakeLabel(string text, int size)
	{
		var l = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		l.AddThemeFontSizeOverride("font_size", size);
		return l;
	}

	private static Button MakeButton(string text)
	{
		var b = new Button { Text = text, CustomMinimumSize = new Vector2(260, 56), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
		b.AddThemeFontSizeOverride("font_size", 28);
		return b;
	}
}
