using System;
using Godot;

namespace PawStorm;

/// <summary>Shared look for the menus (rounded orange buttons, panels, sliders) built in code.</summary>
public static class UiStyle
{
	public static readonly Color Orange = new(1f, 0.6f, 0.15f);
	public static readonly Color Cream = new(1f, 0.93f, 0.75f);
	public static readonly Color Ink = new(0.09f, 0.06f, 0.16f);
	public static readonly Color PanelBg = new(0.1f, 0.08f, 0.2f, 0.82f);

	private static StyleBoxFlat Box(Color bg, Color border, int radius = 16, int borderWidth = 3)
	{
		var s = new StyleBoxFlat { BgColor = bg, BorderColor = border };
		s.SetBorderWidthAll(borderWidth);
		s.SetCornerRadiusAll(radius);
		s.ContentMarginLeft = s.ContentMarginRight = 24;
		s.ContentMarginTop = s.ContentMarginBottom = 10;
		s.ShadowColor = new Color(0, 0, 0, 0.45f);
		s.ShadowSize = 8;
		s.ShadowOffset = new Vector2(0, 4);
		return s;
	}

	public static Button MakeButton(string text, Action onPressed, int fontSize = 30, Vector2? minSize = null)
	{
		var b = new Button
		{
			Text = text,
			CustomMinimumSize = minSize ?? new Vector2(320, 62),
			FocusMode = Control.FocusModeEnum.All,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
		};
		b.AddThemeFontSizeOverride("font_size", fontSize);
		b.AddThemeStyleboxOverride("normal", Box(new Color(0.16f, 0.12f, 0.3f, 0.88f), new Color(1f, 0.6f, 0.15f, 0.6f)));
		b.AddThemeStyleboxOverride("hover", Box(Orange, Cream));
		b.AddThemeStyleboxOverride("focus", Box(Orange, Cream));
		b.AddThemeStyleboxOverride("pressed", Box(new Color(0.85f, 0.45f, 0.08f), Cream));
		b.AddThemeColorOverride("font_color", Cream);
		b.AddThemeColorOverride("font_hover_color", Ink);
		b.AddThemeColorOverride("font_focus_color", Ink);
		b.AddThemeColorOverride("font_pressed_color", Ink);
		b.ProcessMode = Node.ProcessModeEnum.Always;
		b.MouseEntered += () => Hover(b);
		b.FocusEntered += () => Hover(b);
		b.Pressed += () => { Sfx.Play(b, "uiclick", -4f); onPressed?.Invoke(); };
		return b;
	}

	private static void Hover(Button b)
	{
		if (!b.IsInsideTree()) return;
		Sfx.Play(b, "uihover", -10f);
		b.PivotOffset = b.Size / 2f;
		var tw = b.CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
		tw.TweenProperty(b, "scale", new Vector2(1.04f, 1.04f), 0.08f);
		tw.TweenProperty(b, "scale", Vector2.One, 0.12f);
	}

	public static PanelContainer MakePanel(int padding = 28)
	{
		var p = new PanelContainer { ProcessMode = Node.ProcessModeEnum.Always };
		var sb = Box(PanelBg, new Color(1f, 0.6f, 0.15f, 0.9f), 24, 4);
		sb.SetContentMarginAll(padding);
		p.AddThemeStyleboxOverride("panel", sb);
		return p;
	}

	public static Label MakeLabel(string text, int size, Color? color = null, bool outline = false)
	{
		var l = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		l.AddThemeFontSizeOverride("font_size", size);
		l.AddThemeColorOverride("font_color", color ?? Cream);
		if (outline)
		{
			l.AddThemeColorOverride("font_outline_color", Ink);
			l.AddThemeConstantOverride("outline_size", Mathf.Max(4, size / 8));
		}
		return l;
	}

	/// <summary>Row with a label and a 0-100% slider.</summary>
	public static HBoxContainer MakeSlider(string label, float value, Action<float> onChanged)
	{
		var row = new HBoxContainer { ProcessMode = Node.ProcessModeEnum.Always };
		row.AddThemeConstantOverride("separation", 18);
		var l = MakeLabel(label, 26);
		l.CustomMinimumSize = new Vector2(150, 0);
		l.HorizontalAlignment = HorizontalAlignment.Left;
		row.AddChild(l);
		var s = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = value, CustomMinimumSize = new Vector2(320, 36), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
		s.AddThemeStyleboxOverride("slider", Box(new Color(0.16f, 0.12f, 0.3f), new Color(1f, 0.6f, 0.15f, 0.5f), 8, 2));
		s.AddThemeStyleboxOverride("grabber_area", Box(Orange, Orange, 8, 0));
		s.AddThemeStyleboxOverride("grabber_area_highlight", Box(Orange, Cream, 8, 0));
		s.ValueChanged += v => { onChanged?.Invoke((float)v); };
		s.DragEnded += _ => Sfx.Play(s, "uihover", -6f);
		row.AddChild(s);
		return row;
	}
}
