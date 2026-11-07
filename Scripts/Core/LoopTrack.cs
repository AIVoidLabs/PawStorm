using Godot;

namespace PawStorm;

/// <summary>
/// Vertical loop. The node origin is the entry point on the ground, forward is local -Z.
/// The exit lands <see cref="LateralOffset"/> to the side so the ring does not intersect itself.
/// The Area3D shape (child) is the entry trigger.
/// </summary>
public partial class LoopTrack : Area3D
{
	[Export] public float Radius { get; set; } = 8f;
	[Export] public float LateralOffset { get; set; } = -4f;
	[Export] public float RibbonWidth { get; set; } = 3f;

	public Vector3 Forward => -GlobalTransform.Basis.Z.Normalized();

	public override void _Ready()
	{
		BodyEntered += b => { if (b is PlayerController p) p.TryStartLoop(this); };
		BuildVisual();
	}

	private Vector3 Local(float theta)
		=> new Vector3(LateralOffset * theta / Mathf.Tau, Radius * (1f - Mathf.Cos(theta)), -Radius * Mathf.Sin(theta));

	public Vector3 PointAt(float theta) => GlobalTransform * Local(theta);

	public Vector3 TangentAt(float theta)
	{
		Vector3 d = new Vector3(LateralOffset / Mathf.Tau, Radius * Mathf.Sin(theta), -Radius * Mathf.Cos(theta));
		return (GlobalTransform.Basis * d).Normalized();
	}

	private void BuildVisual()
	{
		var mat = new StandardMaterial3D { AlbedoColor = new Color(0.95f, 0.75f, 0.2f) };
		const int segments = 32;
		for (int i = 0; i < segments; i++)
		{
			float t0 = Mathf.Tau * i / segments, t1 = Mathf.Tau * (i + 1) / segments;
			Vector3 a = Local(t0), b = Local(t1);
			Vector3 dir = b - a;
			Vector3 z = new Vector3(0, dir.Y, dir.Z).Normalized() * -1f;
			Vector3 x = Vector3.Right;
			Vector3 y = z.Cross(x);
			var mi = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(RibbonWidth, 0.2f, dir.Length() + 0.05f), Material = mat },
				Transform = new Transform3D(new Basis(x, y, z), (a + b) * 0.5f - y * 0.1f),
			};
			AddChild(mi);
		}
	}
}
