using Godot;

namespace PawStorm;

/// <summary>
/// Boss: Dr. Kennel. Hovers around the arena, telegraphs, charges at the player, then lands stunned.
/// Only hittable while stunned (attack him with Spin, Jump or Pounce). Three hits to win.
/// </summary>
public partial class DrKennel : Area3D, IPounceable
{
	private enum Phase { Hover, Telegraph, Charge, Stunned, Recover, Defeated }

	[Export] public int MaxHp { get; set; } = 3;
	[Export] public float OrbitRadius { get; set; } = 12f;
	[Export] public float OrbitSpeed { get; set; } = 0.12f;
	[Export] public float HoverHeight { get; set; } = 3f;
	[Export] public float TelegraphTime { get; set; } = 1.0f;
	[Export] public float ChargeSpeed { get; set; } = 24f;
	[Export] public float StunTime { get; set; } = 4f;

	private Phase _phase = Phase.Hover;
	private Vector3 _center;
	private Vector3 _chargeTarget;
	private float _t, _orbit, _hover = 4f, _hitCooldown;
	private int _hp;
	private PlayerController _player;

	public override void _Ready()
	{
		_center = new Vector3(GlobalPosition.X, 0, GlobalPosition.Z);
		_hp = MaxHp;
		GameState.BossMaxHp = MaxHp;
		GameState.BossHp = _hp;
		_player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
	}

	public override void _ExitTree() { GameState.BossMaxHp = 0; GameState.BossHp = 0; }

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		_t += dt;
		_hitCooldown -= dt;
		if (_phase == Phase.Defeated || _player == null) return;

		switch (_phase)
		{
			case Phase.Hover:
				_orbit += OrbitSpeed * Mathf.Tau * dt;
				MoveTo(_center + new Vector3(Mathf.Cos(_orbit), 0, Mathf.Sin(_orbit)) * OrbitRadius + Vector3.Up * HoverHeight, 8f, dt);
				FacePlayer();
				if (_t > _hover) SetPhase(Phase.Telegraph);
				break;
			case Phase.Telegraph:
				FacePlayer();
				_chargeTarget = new Vector3(_player.GlobalPosition.X, 0, _player.GlobalPosition.Z);
				if (_t > TelegraphTime) SetPhase(Phase.Charge);
				break;
			case Phase.Charge:
				MoveTo(_chargeTarget, ChargeSpeed, dt);
				if (GlobalPosition.DistanceTo(_chargeTarget) < 0.8f || _t > 2f) SetPhase(Phase.Stunned);
				break;
			case Phase.Stunned:
				if (_t > StunTime) SetPhase(Phase.Recover);
				break;
			case Phase.Recover:
				MoveTo(_center + Vector3.Up * HoverHeight, 8f, dt);
				if (_t > 1f) SetPhase(Phase.Hover);
				break;
		}
		CheckContact();
	}

	private void MoveTo(Vector3 target, float speed, float dt)
		=> GlobalPosition = GlobalPosition.MoveToward(target, speed * dt);

	private void FacePlayer()
	{
		Vector3 d = _player.GlobalPosition - GlobalPosition; d.Y = 0;
		if (d.LengthSquared() > 0.01f) Rotation = new Vector3(0, Mathf.Atan2(-d.X, -d.Z), 0);
	}

	private void SetPhase(Phase p)
	{
		_phase = p;
		_t = 0f;
		switch (p)
		{
			case Phase.Hover: Tint(null); _hover = _hp == 1 ? 2f : _hp == 2 ? 3f : 4f; break;
			case Phase.Telegraph: Tint(new Color(1f, 0.2f, 0.2f)); break;
			case Phase.Charge: break;
			case Phase.Stunned: Tint(new Color(0.4f, 1f, 0.4f)); AddToGroup("pounce_target"); break;
			case Phase.Recover: Tint(null); RemoveFromGroup("pounce_target"); break;
		}
	}

	private void CheckContact()
	{
		foreach (Node3D b in GetOverlappingBodies())
		{
			if (b is not PlayerController p) continue;
			if (_phase == Phase.Stunned && p.IsAttacking)
			{
				if (!p.IsOnFloor() && p.Machine.CurrentId != PlayerStateId.Pounce)
				{
					var v = p.Velocity; v.Y = p.Stats.EnemyBounce * 1.2f; p.Velocity = v;
					p.ResetAirActions();
				}
				Damage();
				return;
			}
			if (_phase != Phase.Stunned && _phase != Phase.Recover) p.TakeHit(GlobalPosition);
		}
	}

	public void Hit()
	{
		if (_phase == Phase.Stunned) Damage();
	}

	private void Damage()
	{
		if (_hitCooldown > 0f) return;
		_hitCooldown = 0.6f;
		_hp--;
		GameState.BossHp = _hp;
		Sfx.Play(this, "hit");
		RemoveFromGroup("pounce_target");
		if (_hp <= 0) { Defeat(); return; }
		SetPhase(Phase.Recover);
	}

	private void Defeat()
	{
		_phase = Phase.Defeated;
		Tint(new Color(1f, 1f, 1f));
		Sfx.Play(this, "core");
		var tween = CreateTween();
		tween.TweenProperty(this, "scale", Vector3.One * 0.01f, 1.2f);
		tween.TweenCallback(Callable.From(() => { GameState.CompleteLevel(); QueueFree(); }));
	}

	private void Tint(Color? color)
	{
		foreach (MeshInstance3D m in ToonModel.Meshes(this))
		{
			m.MaterialOverride = color.HasValue
				? new StandardMaterial3D { AlbedoColor = color.Value, EmissionEnabled = true, Emission = color.Value * 0.6f }
				: null;
		}
	}
}
