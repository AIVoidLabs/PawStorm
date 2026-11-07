using Godot;

namespace PawStorm;

/// <summary>Homing attack: fly straight at the locked target, bounce off it on hit.</summary>
public partial class PounceState : PlayerState
{
	private Node3D _target;
	private float _t;

	public PounceState(PlayerController p) : base(p) { }

	public override void Enter()
	{
		_target = Player.PounceTargetNode;
		_t = 0f;
		Sfx.Play(Player, "pounce");
	}

	public override void Physics(float dt)
	{
		var p = Player;
		if (!GodotObject.IsInstanceValid(_target)) { Machine.Transition(PlayerStateId.Fall); return; }

		Vector3 to = _target.GlobalPosition - (p.GlobalPosition + Vector3.Up * 0.6f);
		_t += dt;
		if (to.Length() < 1.0f)
		{
			if (_target is IPounceable pt) pt.Hit();
			p.ResetAirActions();
			Vector3 h = new Vector3(to.X, 0, to.Z);
			p.Velocity = (h.LengthSquared() > 0.01f ? h.Normalized() : Vector3.Zero) * Stats.PounceSpeed * 0.3f
				+ Vector3.Up * Stats.PounceBounce;
			Machine.Transition(PlayerStateId.Fall);
			return;
		}
		if (_t > Stats.PounceMaxTime) { p.Velocity *= 0.5f; Machine.Transition(PlayerStateId.Fall); return; }

		p.Velocity = to.Normalized() * Stats.PounceSpeed;
		p.MoveAndSlide();
		if (p.GetSlideCollisionCount() > 0)
		{
			p.Velocity = new Vector3(0, 0, 0);
			Machine.Transition(PlayerStateId.Fall);
		}
	}
}
