namespace PawStorm;

public abstract partial class PlayerState
{
	protected PlayerController Player { get; }
	protected PlayerStats Stats => Player.Stats;
	protected StateMachine Machine => Player.Machine;

	protected PlayerState(PlayerController player) { Player = player; }

	public virtual void Enter() { }
	public virtual void Exit() { }
	public abstract void Physics(float dt);
}
