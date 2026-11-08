using Godot;

namespace PawStorm;

/// <summary>Pause overlay (Esc / Start). Created by the HUD; keeps running while the tree is paused.</summary>
public partial class PauseMenu : CanvasLayer
{
	private Control _root;
	private Button _resume;

	public override void _Ready()
	{
		Layer = 10;
		ProcessMode = ProcessModeEnum.Always;
		_root = new Control { Visible = false, ProcessMode = ProcessModeEnum.Always };
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_root);

		var dim = new ColorRect { Color = new Color(0.03f, 0.02f, 0.08f, 0.65f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(dim);

		var panel = UiStyle.MakePanel(36);
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		panel.GrowHorizontal = Control.GrowDirection.Both;
		panel.GrowVertical = Control.GrowDirection.Both;
		_root.AddChild(panel);

		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 18);
		panel.AddChild(v);
		v.AddChild(UiStyle.MakeLabel("Duraklatıldı", 48, new Color(1f, 0.78f, 0.2f), outline: true));
		v.AddChild(UiStyle.MakeSlider("Müzik", AudioSettings.MusicVolume, AudioSettings.SetMusicVolume));
		v.AddChild(UiStyle.MakeSlider("Efektler", AudioSettings.SfxVolume, AudioSettings.SetSfxVolume));
		_resume = UiStyle.MakeButton("Devam", () => SetPaused(false));
		v.AddChild(_resume);
		v.AddChild(UiStyle.MakeButton("Bölümü Yeniden Başlat", () => { SetPaused(false); GameState.BeginLevel(); GetTree().ReloadCurrentScene(); }, 26));
		v.AddChild(UiStyle.MakeButton("Ana Menü", () => { SetPaused(false); GetTree().ChangeSceneToFile(Levels.MainMenu); }));
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
		Music.Duck(paused);
		if (paused) _resume.GrabFocus();
	}
}
