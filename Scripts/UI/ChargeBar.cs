using Godot;

namespace PawStorm;

/// <summary>Three-segment bar for the Pati Sprint charge. Each segment fills in turn; full segments pulse.</summary>
public partial class ChargeBar : Control
{
	public const int Tiers = 3;

	/// <summary>0..1 over the whole bar.</summary>
	public float Ratio { get; set; }

	private static readonly Color[] TierColors =
	{
		new(1.0f, 0.85f, 0.2f), new(1.0f, 0.6f, 0.15f), new(1.0f, 0.3f, 0.2f),
	};

	public override void _Process(double delta)
	{
		bool show = Ratio > 0f;
		if (Visible != show) Visible = show;
		if (show) QueueRedraw();
	}

	public override void _Draw()
	{
		var font = ThemeDB.FallbackFont;
		int filled = Mathf.FloorToInt(Ratio * Tiers + 0.0001f);
		float gap = 10f;
		float segW = (Size.X - gap * (Tiers - 1)) / Tiers;
		float segH = 30f;
		float top = Size.Y - segH;
		float pulse = 0.5f + 0.5f * Mathf.Sin(Time.GetTicksMsec() * 0.018f);

		DrawStringOutline(font, new Vector2(0, top - 12), "PATİ SPRINT", HorizontalAlignment.Center, Size.X, 24, 8, UiStyle.Ink);
		DrawString(font, new Vector2(0, top - 12), "PATİ SPRINT", HorizontalAlignment.Center, Size.X, 24, UiStyle.Cream);
		for (int i = 0; i < Tiers; i++)
		{
			var rect = new Rect2(i * (segW + gap), top, segW, segH);
			DrawStyleBox(Box(new Color(0.08f, 0.06f, 0.16f, 0.85f), new Color(1f, 0.93f, 0.75f, 0.7f), 2), rect);
			float fill = Mathf.Clamp(Ratio * Tiers - i, 0f, 1f);
			if (fill <= 0f) continue;
			bool done = i < filled;
			var color = TierColors[i].Lerp(Colors.White, done ? 0.35f * pulse : 0f);
			var inner = new Rect2(rect.Position + new Vector2(3, 3), new Vector2((segW - 6) * fill, segH - 6));
			DrawStyleBox(Box(color, color, 0), inner);
		}
		if (filled > 0)
		{
			DrawStringOutline(font, new Vector2(0, top + segH + 26), $"x{filled}", HorizontalAlignment.Center, Size.X, 22, 8, UiStyle.Ink);
			DrawString(font, new Vector2(0, top + segH + 26), $"x{filled}", HorizontalAlignment.Center, Size.X, 22, TierColors[Mathf.Min(filled, Tiers) - 1].Lerp(Colors.White, 0.4f));
		}
	}

	private static StyleBoxFlat Box(Color bg, Color border, int borderWidth)
	{
		var s = new StyleBoxFlat { BgColor = bg, BorderColor = border };
		s.SetBorderWidthAll(borderWidth);
		s.SetCornerRadiusAll(8);
		return s;
	}
}
