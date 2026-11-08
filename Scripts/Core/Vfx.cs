using Godot;

namespace PawStorm;

/// <summary>Hit / defeat effects built from code: particle bursts, flash, shockwave ring, floating text, hit-stop and camera shake.</summary>
public static class Vfx
{
	private static bool _hitStopActive;

	// ---------------------------------------------------------------- public effects
	/// <summary>Regular enemy defeated: brown debris + yellow sparks + white puff, "+N" popup, small shake and hit-stop.</summary>
	public static void EnemyDefeat(Node ctx, Vector3 pos, int reward)
	{
		Flash(ctx, pos, new Color(1f, 1f, 0.9f), 0.7f, 0.2f);
		Ring(ctx, pos - Vector3.Up * 0.6f, new Color(1f, 0.9f, 0.4f), 3.2f, 0.4f);
		Burst(ctx, pos, 22, 0.7f, 3f, 8f, 0.14f, new Color(0.65f, 0.4f, 0.22f), new Color(0.4f, 0.25f, 0.15f), -14f, cube: true);
		Burst(ctx, pos, 26, 0.55f, 4f, 10f, 0.08f, new Color(1f, 0.9f, 0.3f), new Color(1f, 0.5f, 0.1f), -4f);
		Burst(ctx, pos, 8, 0.5f, 1f, 3f, 0.3f, new Color(1f, 1f, 1f), new Color(0.8f, 0.8f, 0.9f), 0f);
		Popup(ctx, pos + Vector3.Up * 0.8f, $"+{reward}", new Color(1f, 0.9f, 0.3f));
		Shake(ctx, 0.07f, 0.18f);
		HitStop(ctx, 0.06f);
		Sfx.Play(ctx, "hit", -2f, 0.1f);
	}

	/// <summary>Smaller pop for targets / props that are not enemies.</summary>
	public static void TargetPop(Node ctx, Vector3 pos, int reward)
	{
		Flash(ctx, pos, new Color(1f, 0.7f, 0.7f), 0.55f, 0.18f);
		Ring(ctx, pos, new Color(1f, 0.4f, 0.4f), 2.6f, 0.35f);
		Burst(ctx, pos, 20, 0.55f, 3f, 8f, 0.09f, new Color(1f, 0.35f, 0.3f), new Color(1f, 0.8f, 0.3f), -6f);
		if (reward > 0) Popup(ctx, pos + Vector3.Up * 0.8f, $"+{reward}", new Color(1f, 0.9f, 0.3f));
		Shake(ctx, 0.05f, 0.12f);
	}

	/// <summary>Boss takes a hit: big white/yellow impact, heavier shake and hit-stop.</summary>
	public static void BossHit(Node ctx, Vector3 pos)
	{
		Flash(ctx, pos, Colors.White, 1.6f, 0.25f);
		Ring(ctx, pos, new Color(1f, 0.95f, 0.5f), 6f, 0.5f);
		Burst(ctx, pos, 40, 0.8f, 5f, 12f, 0.12f, new Color(1f, 0.95f, 0.4f), new Color(1f, 0.5f, 0.15f), -8f);
		Burst(ctx, pos, 18, 0.7f, 2f, 6f, 0.2f, new Color(0.95f, 0.95f, 1f), new Color(0.6f, 0.6f, 0.8f), 0f);
		Popup(ctx, pos + Vector3.Up * 2.5f, "HIT!", new Color(1f, 0.9f, 0.3f));
		Shake(ctx, 0.18f, 0.35f);
		HitStop(ctx, 0.1f);
	}

	/// <summary>Boss defeated: a chain of explosions.</summary>
	public static void BossDefeat(Node ctx, Vector3 pos)
	{
		for (int i = 0; i < 6; i++)
		{
			float delay = i * 0.14f;
			var offset = new Vector3(GD.Randf() * 3f - 1.5f, GD.Randf() * 2f, GD.Randf() * 3f - 1.5f);
			ctx.GetTree().CreateTimer(delay).Timeout += () =>
			{
				if (!GodotObject.IsInstanceValid(ctx)) return;
				Flash(ctx, pos + offset, new Color(1f, 0.85f, 0.4f), 1.4f, 0.3f);
				Ring(ctx, pos + offset, new Color(1f, 0.6f, 0.2f), 5f, 0.5f);
				Burst(ctx, pos + offset, 30, 0.9f, 4f, 11f, 0.14f, new Color(1f, 0.8f, 0.3f), new Color(0.9f, 0.3f, 0.1f), -6f);
				Sfx.Play(ctx, "hit", 0f, 0.15f);
				Shake(ctx, 0.2f, 0.3f);
			};
		}
		Popup(ctx, pos + Vector3.Up * 3.2f, "K.O.!", new Color(1f, 0.9f, 0.3f), 1.4f);
	}

	// ---------------------------------------------------------------- building blocks
	private static Node Parent(Node ctx) => ctx.GetTree().CurrentScene ?? ctx.GetTree().Root;

	private static void Spawn(Node ctx, Node3D node, Vector3 pos, float life)
	{
		var tree = ctx.GetTree();
		var parent = Parent(ctx);
		Callable.From(() => { parent.AddChild(node); node.GlobalPosition = pos; }).CallDeferred();
		tree.CreateTimer(life).Timeout += () => { if (GodotObject.IsInstanceValid(node)) node.QueueFree(); };
	}

	private static StandardMaterial3D Unshaded(Color c, bool vertexColor = false) => new()
	{
		ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
		AlbedoColor = c,
		VertexColorUseAsAlbedo = vertexColor,
		Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		NoDepthTest = false,
	};

	public static void Burst(Node ctx, Vector3 pos, int count, float life, float speedMin, float speedMax, float size,
		Color from, Color to, float gravityY, bool cube = false)
	{
		var burst = DebrisBurst.Create(count, life, speedMin, speedMax, size, from, to, gravityY, cube);
		Spawn(ctx, burst, pos, life + 0.4f);
	}
	/// <summary>Quick expanding, fading white-ish sphere ("pop").</summary>
	public static void Flash(Node ctx, Vector3 pos, Color c, float radius, float duration)
	{
		var mat = Unshaded(new Color(c, 0.9f));
		var mi = new MeshInstance3D { Mesh = new SphereMesh { Radius = radius, Height = radius * 2f, RadialSegments = 12, Rings = 6, Material = mat }, Scale = Vector3.One * 0.3f };
		Spawn(ctx, mi, pos, duration + 0.1f);
		var light = new OmniLight3D { LightColor = c, OmniRange = 6f, LightEnergy = 4f };
		mi.AddChild(light);
		var tw = mi.CreateTween().SetParallel();
		tw.TweenProperty(mi, "scale", Vector3.One * 1.6f, duration).SetEase(Tween.EaseType.Out);
		tw.TweenProperty(mat, "albedo_color:a", 0f, duration);
		tw.TweenProperty(light, "light_energy", 0f, duration);
	}

	/// <summary>Flat shockwave ring on the XZ plane.</summary>
	public static void Ring(Node ctx, Vector3 pos, Color c, float endScale, float duration)
	{
		var mat = Unshaded(new Color(c, 0.85f));
		var mi = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 0.42f, OuterRadius = 0.5f, Material = mat }, Scale = Vector3.One * 0.4f };
		Spawn(ctx, mi, pos, duration + 0.1f);
		var tw = mi.CreateTween().SetParallel();
		tw.TweenProperty(mi, "scale", new Vector3(endScale, endScale * 0.6f, endScale), duration).SetEase(Tween.EaseType.Out);
		tw.TweenProperty(mat, "albedo_color:a", 0f, duration);
	}

	/// <summary>Floating "+N" text that rises and fades.</summary>
	public static void Popup(Node ctx, Vector3 pos, string text, Color color, float scale = 1f)
	{
		var label = new Label3D
		{
			Text = text, FontSize = (int)(72 * scale), OutlineSize = 18, Modulate = color, OutlineModulate = new Color(0.1f, 0.05f, 0.15f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = true, PixelSize = 0.01f, Shaded = false,
			Scale = Vector3.One * 0.3f,
		};
		Spawn(ctx, label, pos, 1.1f);
		var tw = label.CreateTween().SetParallel();
		tw.TweenProperty(label, "scale", Vector3.One, 0.18f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		tw.TweenProperty(label, "position:y", pos.Y + 1.6f, 0.9f).SetEase(Tween.EaseType.Out);
		tw.TweenProperty(label, "modulate:a", 0f, 0.4f).SetDelay(0.6f);
		tw.TweenProperty(label, "outline_modulate:a", 0f, 0.4f).SetDelay(0.6f);
	}

	/// <summary>Brief slow-motion "freeze" on impact (real-time timer, so it always ends).</summary>
	public static void HitStop(Node ctx, float seconds)
	{
		if (_hitStopActive || !ctx.IsInsideTree()) return;
		_hitStopActive = true;
		Engine.TimeScale = 0.12;
		ctx.GetTree().CreateTimer(seconds, true, false, true).Timeout += () => { Engine.TimeScale = 1.0; _hitStopActive = false; };
	}

	public static void Shake(Node ctx, float amplitude, float duration)
	{
		if (ctx.GetTree().GetFirstNodeInGroup("follow_camera") is FollowCamera cam) cam.Shake(amplitude, duration);
	}
}
