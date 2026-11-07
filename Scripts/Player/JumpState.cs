using Godot;

namespace PawStorm;

public partial class JumpState : PlayerState
{
	private bool _cut;

	public JumpState(PlayerController p) : base(p) { }

	public override void Enter()
	{
		_cut = false;
		var v = Player.Velocity;
		v.Y = Stats.JumpVelocity;
		Player.Velocity = v;
		Player.ConsumeJump();
	}

	public override void Physics(float dt)
	{
		var p = Player;
		// Variable jump height: releasing the button early cuts upward speed.
		if (!_cut && !Input.IsActionPressed("jump") && p.Velocity.Y > 0f)
		{
			var v = p.Velocity;
			v.Y *= Stats.JumpCutMultiplier;
			p.Velocity = v;
			_cut = true;
		}
		if (p.JumpPressed() && p.TryPounce()) return;
		p.ApplyAirMovement(dt);
		p.MoveAndSlide();
		if (p.TryStartGrind()) return;
		if (p.IsOnFloor() && p.Velocity.Y <= 0.1f) { p.Land(); return; }
		if (p.Velocity.Y <= 0f) Machine.Transition(PlayerStateId.Fall);
	}
}
