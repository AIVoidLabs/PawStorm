using Godot;

namespace PawStorm;

/// <summary>Touching it makes this the respawn point after a lost life.</summary>
public partial class Checkpoint : Area3D
{
	private bool _active;

	public override void _Ready() => BodyEntered += OnBodyEntered;

	private void OnBodyEntered(Node3D body)
	{
		if (_active || body is not PlayerController p) return;
		_active = true;
		p.SetCheckpoint(GlobalPosition + Vector3.Up * 0.5f);
		Sfx.Play(this, "checkpoint");
		foreach (Node c in GetChildren())
			if (c is MeshInstance3D m) m.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 1f, 0.4f) };
	}
}
