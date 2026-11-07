namespace PawStorm;

public partial class RunState : PlayerState
{
	public RunState(PlayerController p) : base(p) { }

	public override void Physics(float dt)
	{
		var p = Player;
		p.ApplyGroundMovement(dt);
		p.MoveAndSlide();
		if (!p.IsOnFloor()) { Machine.Transition(PlayerStateId.Fall); return; }
		if (p.JumpPressed()) { Machine.Transition(PlayerStateId.Jump); return; }
		if (p.SpinPressed())
		{
			Machine.Transition(p.HorizontalSpeed > Stats.ChargeMaxEntrySpeed ? PlayerStateId.Spin : PlayerStateId.Charge);
			return;
		}
		if (p.InputDir.LengthSquared() < 0.01f && p.HorizontalSpeed < 0.3f)
			Machine.Transition(PlayerStateId.Idle);
	}
}
