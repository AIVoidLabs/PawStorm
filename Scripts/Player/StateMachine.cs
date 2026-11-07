using System.Collections.Generic;

namespace PawStorm;

public enum PlayerStateId { Idle, Run, Jump, Fall, Charge, Spin, Pounce, Grind, WallClimb, Loop, Hurt }

public partial class StateMachine
{
	private readonly Dictionary<PlayerStateId, PlayerState> _states = new();
	public PlayerState Current { get; private set; }
	public PlayerStateId CurrentId { get; private set; }
	public bool PreviousWasGround { get; private set; }

	public void Register(PlayerStateId id, PlayerState state) => _states[id] = state;

	public void Transition(PlayerStateId id)
	{
		if (Current != null && CurrentId == id) return;
		PreviousWasGround = Current is IdleState || Current is RunState || Current is SpinState;
		Current?.Exit();
		CurrentId = id;
		Current = _states[id];
		Current.Enter();
	}

	public void Physics(float dt) => Current?.Physics(dt);
}
