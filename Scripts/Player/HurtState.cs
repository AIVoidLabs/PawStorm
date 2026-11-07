using Godot;

namespace PawStorm;

/// <summary>Knocked back after a hit: no control until landed (and a short minimum time).</summary>
public partial class HurtState : PlayerState
{
	private float _t;

	public HurtState(PlayerController p) : base(p) { }

	public override void Enter()
	{
		_t = 0f;
		Vector3 d = Player.HurtDirection;
		Player.Velocity = d * Stats.HurtKnockback + Vector3.Up * Stats.HurtUpSpeed;
	}

	public override void Physics(float dt)
	{
		var p = Player;
		_t += dt;
		var v = p.Velocity;
		v.Y = Mathf.Max(v.Y - Stats.Gravity * dt, -Stats.MaxFallSpeed);
		p.Velocity = v;
		p.MoveAndSlide();
		if (_t > Stats.HurtMinTime && p.IsOnFloor()) p.Land();
	}
}
