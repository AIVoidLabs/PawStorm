namespace PawStorm;

public partial class IdleState : PlayerState
{
	public IdleState(PlayerController p) : base(p) { }

	public override void Physics(float dt)
	{
		var p = Player;
		p.ApplyGroundMovement(dt);
		p.MoveAndSlide();
		if (!p.IsOnFloor()) { Machine.Transition(PlayerStateId.Fall); return; }
		if (p.JumpPressed()) { Machine.Transition(PlayerStateId.Jump); return; }
		if (p.InputDir.LengthSquared() > 0.01f || p.HorizontalSpeed > 0.5f)
			Machine.Transition(PlayerStateId.Run);
	}
}
