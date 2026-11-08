using Godot;

namespace PawStorm;

/// <summary>
/// Final boss: General Growl, an armoured robot wolf with a laser cannon arm. He keeps his distance and strafes along
/// the back of the arena, telegraphs, and fires plasma balls: a single aimed ball, then a triple spread, then rings.
/// Weak points: send his balls back (Pounce them or hit them while attacking), or hit him while he vents after a volley.
/// </summary>
public partial class GeneralGrowl : Area3D, IPounceable
{
	private enum Phase { Intro, Strafe, Aim, Fire, Vent, Stagger, Defeated }

	[Export] public int MaxHp { get; set; } = 6;
	[Export] public float StrafeHalfWidth { get; set; } = 22f;
	[Export] public float WalkSpeed { get; set; } = 7f;
	/// <summary>Where the plasma ball leaves the cannon, in boss space (Godot: -Z is forward).</summary>
	[Export] public Vector3 MuzzleOffset { get; set; } = new Vector3(1.8f, 4.0f, -2.6f);

	public Vector3 ChestPosition => GlobalPosition + Vector3.Up * 3.4f;

	private Phase _phase = Phase.Intro;
	private float _t, _hitCooldown, _strafeTarget;
	private int _hp, _shotsInVolley, _volleyCount;
	private PlayerController _player;
	private ModelAnimator _anim;
	private MeshInstance3D _aimLine;
	private float _ventTimer;

	public override void _Ready()
	{
		_hp = MaxHp;
		GameState.BossMaxHp = MaxHp;
		GameState.BossHp = _hp;
		GameState.BossName = "General Growl";
		_player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		_anim = new ModelAnimator(this);
		_anim.Play("Idle");
		_aimLine = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(0.08f, 0.08f, 1f), Material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(1f, 0.2f, 0.15f, 0.7f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha } },
			TopLevel = true, Visible = false,
		};
		AddChild(_aimLine);
	}

	public override void _ExitTree() { GameState.BossMaxHp = 0; GameState.BossHp = 0; GameState.BossName = ""; }

	// ---------------------------------------------------------------- behaviour
	private int Stage => _hp >= 5 ? 1 : _hp >= 3 ? 2 : 3;
	private float ShotInterval => Stage == 1 ? 0.9f : Stage == 2 ? 0.8f : 0.7f;
	private float VentTime => Stage == 1 ? 4.2f : Stage == 2 ? 3.6f : 3.2f;
	private const int ShotsPerVolley = 3;

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		_t += dt;
		_hitCooldown -= dt;
		if (_phase == Phase.Defeated || _player == null) return;

		switch (_phase)
		{
			case Phase.Intro:
				FacePlayer(dt);
				if (_t > 1.6f) PickStrafe();
				break;
			case Phase.Strafe:
				FacePlayer(dt);
				WalkToward(dt);
				if (Mathf.Abs(GlobalPosition.X - _strafeTarget) < 0.4f || _t > 2.4f) BeginAim();
				break;
			case Phase.Aim:
				FacePlayer(dt);
				UpdateAimLine();
				if (_t > 0.75f) Fire();
				break;
			case Phase.Fire:
				FacePlayer(dt);
				if (_t > ShotInterval * 0.5f)
				{
					if (_shotsInVolley >= ShotsPerVolley) BeginVent();
					else PickStrafe();
				}
				break;
			case Phase.Vent:
				_ventTimer -= dt;
				if (_ventTimer <= 0f) PickStrafe();
				else if (Mathf.PosMod(_t, 0.7f) < dt) Vfx.Burst(this, GlobalPosition + Vector3.Up * 4.5f, 8, 0.7f, 1f, 3f, 0.22f, new Color(0.9f, 0.95f, 1f), new Color(0.6f, 0.65f, 0.8f), 2f);
				break;
			case Phase.Stagger:
				if (_t > 0.9f) PickStrafe();
				break;
		}
		CheckContact();
	}

	private void SetPhase(Phase p)
	{
		_phase = p;
		_t = 0f;
		_aimLine.Visible = p == Phase.Aim;
		switch (p)
		{
			case Phase.Strafe: _anim.Play("Walk", 1.1f); break;
			case Phase.Aim: _anim.Play("Aim"); Sfx.Play(this, "boss_warn", -5f); break;
			case Phase.Fire: _anim.Play("Fire", 1.3f, 0.05f); break;
			case Phase.Vent: _anim.Play("Vent"); AddToGroup("pounce_target"); Sfx.Play(this, "vent", -2f); break;
			case Phase.Stagger: _anim.Play("Vent"); break;
			default: _anim.Play("Idle"); break;
		}
		if (p != Phase.Vent) RemoveFromGroup("pounce_target");
	}

	private void PickStrafe()
	{
		float range = StrafeHalfWidth;
		_strafeTarget = Mathf.Clamp((float)GD.RandRange(-range, range), -range, range);
		if (Mathf.Abs(_strafeTarget - GlobalPosition.X) < 6f) _strafeTarget = GlobalPosition.X > 0 ? -range * 0.7f : range * 0.7f;
		SetPhase(Phase.Strafe);
	}

	private void WalkToward(float dt)
	{
		var pos = GlobalPosition;
		pos.X = Mathf.MoveToward(pos.X, _strafeTarget, WalkSpeed * dt);
		GlobalPosition = pos;
	}

	private void BeginAim() => SetPhase(Phase.Aim);

	private void BeginVent()
	{
		_shotsInVolley = 0;
		_ventTimer = VentTime;
		SetPhase(Phase.Vent);
	}

	private Vector3 Muzzle => GlobalTransform * MuzzleOffset;

	private void UpdateAimLine()
	{
		Vector3 from = Muzzle, to = _player.GlobalPosition + Vector3.Up * 0.7f;
		Vector3 d = to - from;
		float len = d.Length();
		if (len < 0.1f) return;
		((BoxMesh)_aimLine.Mesh).Size = new Vector3(0.08f, 0.08f, len);
		_aimLine.GlobalTransform = new Transform3D(Basis.LookingAt(d.Normalized(), Vector3.Up), from + d * 0.5f);
	}

	private void Fire()
	{
		_shotsInVolley++;
		_volleyCount++;
		Vector3 from = Muzzle;
		Sfx.Play(this, "laser_fire", -3f);
		Vfx.Flash(this, from, new Color(1f, 0.6f, 0.2f), 0.7f, 0.15f);
		Vfx.Shake(this, 0.05f, 0.1f);

		float speed = Stage == 1 ? 13f : Stage == 2 ? 15.5f : 18f;
		// lead the target a little
		Vector3 target = _player.GlobalPosition + Vector3.Up * 0.7f + new Vector3(_player.Velocity.X, 0, _player.Velocity.Z) * (from.DistanceTo(_player.GlobalPosition) / speed) * 0.6f;
		Vector3 aim = (target - from).Normalized();

		switch (Stage)
		{
			case 1:
				LaserBall.Spawn(this, this, from, aim * speed);
				break;
			case 2:
				foreach (float deg in new[] { -17f, 0f, 17f })
					LaserBall.Spawn(this, this, from, aim.Rotated(Vector3.Up, Mathf.DegToRad(deg)) * speed);
				break;
			default:
				// alternate: a fast aimed ball and a slow ring of balls around the boss
				if (_volleyCount % 2 == 1)
					LaserBall.Spawn(this, this, from, aim * speed);
				else
					for (int i = 0; i < 10; i++)
					{
						float a = Mathf.Tau * i / 10f;
						var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
						LaserBall.Spawn(this, this, GlobalPosition + Vector3.Up * 1.2f + dir * 2.5f, dir * 8.5f);
					}
				break;
		}
		SetPhase(Phase.Fire);
	}

	private void FacePlayer(float dt)
	{
		Vector3 d = _player.GlobalPosition - GlobalPosition; d.Y = 0;
		if (d.LengthSquared() < 0.01f) return;
		float target = Mathf.Atan2(-d.X, -d.Z);
		Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, target, Mathf.Clamp(8f * dt, 0f, 1f)), 0);
	}

	// ---------------------------------------------------------------- damage
	private void CheckContact()
	{
		foreach (Node3D b in GetOverlappingBodies())
		{
			if (b is not PlayerController p) continue;
			if (_phase == Phase.Vent && p.IsAttacking)
			{
				if (!p.IsOnFloor() && p.Machine.CurrentId != PlayerStateId.Pounce)
				{
					var v = p.Velocity; v.Y = p.Stats.EnemyBounce * 1.2f; p.Velocity = v;
					p.ResetAirActions();
				}
				Damage();
				return;
			}
			if (_phase != Phase.Vent && _phase != Phase.Stagger) p.TakeHit(GlobalPosition);
		}
	}

	/// <summary>Pounce reached him while he is venting.</summary>
	public void Hit()
	{
		if (_phase == Phase.Vent) Damage();
	}

	/// <summary>One of his own plasma balls came back.</summary>
	public void OnReflectedHit()
	{
		if (_phase != Phase.Defeated) Damage();
	}

	private void Damage()
	{
		if (_hitCooldown > 0f || _phase == Phase.Defeated) return;
		_hitCooldown = 0.7f;
		_hp--;
		GameState.BossHp = _hp;
		Sfx.Play(this, "hit");
		Vfx.BossHit(this, ChestPosition);
		if (_hp <= 0) { Defeat(); return; }
		_shotsInVolley = 0;
		SetPhase(Phase.Stagger);
	}

	private void Defeat()
	{
		_phase = Phase.Defeated;
		_aimLine.Visible = false;
		RemoveFromGroup("pounce_target");
		_anim.Play("Vent");
		Sfx.Play(this, "core");
		Vfx.BossDefeat(this, ChestPosition);
		var tween = CreateTween();
		tween.TweenProperty(this, "scale", Vector3.One * 0.01f, 1.4f).SetDelay(0.5f);
		tween.TweenCallback(Callable.From(() => { GameState.CompleteLevel(false); QueueFree(); }));
	}
}
