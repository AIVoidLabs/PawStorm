using Godot;

namespace PawStorm;

/// <summary>
/// Title screen: slowly zooming background shots of every region, logo, styled buttons,
/// level select and audio settings. Everything is built in code so the scene stays a single node.
/// </summary>
public partial class MainMenu : Control
{
	private const float SlideSeconds = 7f;
	private const float CrossfadeSeconds = 1.5f;

	private Texture2D[] _backgrounds;
	private TextureRect _back, _front;
	private float _slideTime;
	private int _index;

	private Control _mainColumn, _levelPanel, _settingsPanel;
	private Button _playButton, _levelBack, _settingsBack;
	private ColorRect _fade;
	private bool _leaving;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().Paused = false;
		AudioSettings.Init();
		Music.Play(this, "menu");
		SetAnchorsPreset(LayoutPreset.FullRect);

		BuildBackground();
		BuildMainColumn();
		BuildLevelPanel();
		BuildSettingsPanel();
		BuildFooter();

		_fade = new ColorRect { Color = Colors.Black, MouseFilter = MouseFilterEnum.Ignore };
		_fade.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(_fade);
		CreateTween().TweenProperty(_fade, "color:a", 0f, 0.6f);

		_playButton.GrabFocus();
	}

	// ---------------------------------------------------------------- background
	private void BuildBackground()
	{
		_backgrounds = new Texture2D[Levels.All.Length];
		for (int i = 0; i < Levels.All.Length; i++)
			_backgrounds[i] = GD.Load<Texture2D>($"res://Art/Textures/Menu/menu_{Levels.All[i].Preview}.png");

		_back = MakeBackgroundRect(_backgrounds[0]);
		_front = MakeBackgroundRect(_backgrounds[1 % _backgrounds.Length]);
		_front.Modulate = new Color(1, 1, 1, 0);
		AddChild(_back);
		AddChild(_front);
		_index = 1 % _backgrounds.Length;

		// darken the left side so the menu stays readable
		var shade = new TextureRect
		{
			Texture = new GradientTexture2D
			{
				Gradient = new Gradient { Colors = new[] { new Color(0.05f, 0.03f, 0.12f, 0.9f), new Color(0.05f, 0.03f, 0.12f, 0f) }, Offsets = new[] { 0f, 0.75f } },
				FillFrom = new Vector2(0, 0), FillTo = new Vector2(1, 0), Width = 256, Height = 8,
			},
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = MouseFilterEnum.Ignore,
		};
		shade.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(shade);
	}

	private static TextureRect MakeBackgroundRect(Texture2D tex)
	{
		var r = new TextureRect
		{
			Texture = tex, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = MouseFilterEnum.Ignore,
		};
		r.SetAnchorsPreset(LayoutPreset.FullRect);
		return r;
	}

	public override void _Process(double delta)
	{
		if (_back == null) return;
		_slideTime += (float)delta;
		// Push the cat towards the right third (scale about a pivot left of centre) and zoom slowly.
		var pivot = new Vector2(Size.X * 0.1f, Size.Y * 0.6f);
		float zoom = 1.3f + 0.1f * Mathf.Clamp(_slideTime / SlideSeconds, 0f, 1f);
		_back.PivotOffset = _front.PivotOffset = pivot;
		_back.Scale = new Vector2(1.4f, 1.4f);
		_front.Scale = new Vector2(zoom, zoom);
		_front.Modulate = new Color(1, 1, 1, Mathf.Clamp(_slideTime / CrossfadeSeconds, 0f, 1f));

		if (_slideTime >= SlideSeconds)
		{
			_back.Texture = _front.Texture;
			_index = (_index + 1) % _backgrounds.Length;
			_front.Texture = _backgrounds[_index];
			_slideTime = 0f;
		}
	}

	// ---------------------------------------------------------------- main column
	private void BuildMainColumn()
	{
		var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		column.SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide);
		column.OffsetLeft = 80;
		column.OffsetRight = 520;
		column.AddThemeConstantOverride("separation", 16);
		AddChild(column);
		_mainColumn = column;

		var title = UiStyle.MakeLabel("PawStorm", 104, new Color(1f, 0.78f, 0.2f), outline: true);
		title.HorizontalAlignment = HorizontalAlignment.Left;
		title.AddThemeConstantOverride("outline_size", 18);
		title.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.5f));
		title.AddThemeConstantOverride("shadow_offset_y", 8);
		column.AddChild(title);
		var tag = UiStyle.MakeLabel("Pati ile fırtınayı yakala, kedileri kurtar!", 24, UiStyle.Cream, outline: true);
		tag.HorizontalAlignment = HorizontalAlignment.Left;
		column.AddChild(tag);
		column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 22) });

		_playButton = UiStyle.MakeButton("Oyna", () => Launch(Levels.First));
		column.AddChild(_playButton);
		column.AddChild(UiStyle.MakeButton("Bölüm Seç", () => ShowPanel(_levelPanel, _levelBack)));
		column.AddChild(UiStyle.MakeButton("Ayarlar", () => ShowPanel(_settingsPanel, _settingsBack)));
		column.AddChild(UiStyle.MakeButton("Çıkış", () => GetTree().Quit()));
	}

	private void BuildFooter()
	{
		var hint = UiStyle.MakeLabel(
			"WASD / sol analog: hareket   Space / A: zıpla (havada Pounce, Air Twist)   Shift / X: Pati Sprint   Fare / sağ analog: kamera   Esc: duraklat",
			16, new Color(1f, 0.93f, 0.75f, 0.85f), outline: true);
		hint.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
		hint.OffsetTop = -44;
		hint.OffsetBottom = -12;
		AddChild(hint);
	}

	// ---------------------------------------------------------------- panels
	private void BuildLevelPanel()
	{
		var panel = UiStyle.MakePanel();
		panel.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
		panel.GrowHorizontal = GrowDirection.Both;
		panel.GrowVertical = GrowDirection.Both;
		panel.Visible = false;
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 18);
		panel.AddChild(v);
		v.AddChild(UiStyle.MakeLabel("Bölüm Seç", 44, new Color(1f, 0.78f, 0.2f), outline: true));

		var grid = new GridContainer { Columns = 5 };
		grid.AddThemeConstantOverride("h_separation", 16);
		grid.AddThemeConstantOverride("v_separation", 16);
		v.AddChild(grid);
		for (int i = 0; i < Levels.All.Length; i++)
		{
			var info = Levels.All[i];
			var b = UiStyle.MakeButton($"{i + 1}. {info.Title}", () => Launch(info.Path), 18, new Vector2(210, 160));
			b.Icon = _backgrounds[i];
			b.ExpandIcon = true;
			b.VerticalIconAlignment = VerticalAlignment.Top;
			b.IconAlignment = HorizontalAlignment.Center;
			b.AddThemeConstantOverride("icon_max_width", 190);
			grid.AddChild(b);
		}
		_levelBack = UiStyle.MakeButton("Geri", HidePanels, 28, new Vector2(240, 54));
		_levelBack.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		v.AddChild(_levelBack);
		AddChild(panel);
		_levelPanel = panel;
	}

	private void BuildSettingsPanel()
	{
		var panel = UiStyle.MakePanel(36);
		panel.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
		panel.GrowHorizontal = GrowDirection.Both;
		panel.GrowVertical = GrowDirection.Both;
		panel.Visible = false;
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 24);
		panel.AddChild(v);
		v.AddChild(UiStyle.MakeLabel("Ayarlar", 44, new Color(1f, 0.78f, 0.2f), outline: true));
		v.AddChild(UiStyle.MakeSlider("Müzik", AudioSettings.MusicVolume, AudioSettings.SetMusicVolume));
		v.AddChild(UiStyle.MakeSlider("Efektler", AudioSettings.SfxVolume, AudioSettings.SetSfxVolume));
		_settingsBack = UiStyle.MakeButton("Geri", HidePanels, 28, new Vector2(240, 54));
		_settingsBack.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		v.AddChild(_settingsBack);
		AddChild(panel);
		_settingsPanel = panel;
	}

	private void ShowPanel(Control panel, Control focus)
	{
		_mainColumn.Visible = false;
		panel.Visible = true;
		focus.GrabFocus();
	}

	private void HidePanels()
	{
		_levelPanel.Visible = _settingsPanel.Visible = false;
		_mainColumn.Visible = true;
		_playButton.GrabFocus();
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (e.IsActionPressed("ui_cancel") && (_levelPanel.Visible || _settingsPanel.Visible))
		{
			HidePanels();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Launch(string scenePath)
	{
		if (_leaving) return;
		_leaving = true;
		GameState.ResetAll();
		var tw = CreateTween();
		tw.TweenProperty(_fade, "color:a", 1f, 0.35f);
		tw.TweenCallback(Callable.From(() => GetTree().ChangeSceneToFile(scenePath)));
	}
}
