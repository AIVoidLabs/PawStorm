namespace PawStorm;

/// <summary>Rolling ball: low friction, extra slope gain, weak steering. Ends when slow or on jump.</summary>
public partial class SpinState : PlayerState
{
	public SpinState(PlayerController p) : base(p) { }

	public override void Physics(float dt)
	{
		var p = Player;
		p.ApplyRollMovement(dt);
		p.MoveAndSlide();
		if (!p.IsOnFloor()) { Machine.Transition(PlayerStateId.Fall); return; }
		if (p.JumpPressed()) { Machine.Transition(PlayerStateId.Jump); return; }
		if (p.SpinPressed() || p.HorizontalSpeed < Stats.RollMinSpeed) p.Land();
	}
}
