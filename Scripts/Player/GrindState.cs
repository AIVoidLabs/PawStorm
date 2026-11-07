using Godot;

namespace PawStorm;

/// <summary>Balancing on a rail/wire: speed follows the rail, gravity along it, jump to leave.</summary>
public partial class GrindState : PlayerState
{
	private GrindRail _rail;
	private float _offset;
	private float _dir;
	private float _speed;

	public GrindState(PlayerController p) : base(p) { }

	public override void Enter()
	{
		_rail = Player.CurrentRail;
		_offset = Player.RailOffset;
		Vector3 tangent = _rail.WorldTangentAt(_offset);
		float along = Player.Velocity.Dot(tangent);
		_dir = along >= 0f ? 1f : -1f;
		_speed = Mathf.Max(Mathf.Abs(along), Stats.GrindMinSpeed);
	}

	public override void Physics(float dt)
	{
		var p = Player;
		Vector3 d = _rail.WorldTangentAt(_offset) * _dir;
		// Downhill (d.Y < 0) speeds up, uphill slows down, never below the minimum.
		_speed = Mathf.Max(Stats.GrindMinSpeed * 0.5f, _speed - d.Y * Stats.Gravity * Stats.GrindGravityFactor * dt);
		_offset += _dir * _speed * dt;
		p.Velocity = d * _speed;

		if (_offset <= 0f || _offset >= _rail.Length)
		{
			p.StartGrindCooldown();
			Machine.Transition(PlayerStateId.Fall);
			return;
		}
		p.GlobalPosition = _rail.WorldPointAt(_offset);

		if (p.JumpPressed())
		{
			p.StartGrindCooldown();
			Machine.Transition(PlayerStateId.Jump);
		}
	}
}
