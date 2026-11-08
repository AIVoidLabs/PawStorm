using Godot;

namespace PawStorm;

/// <summary>
/// Dresses the collision greybox: every CsgBox3D under "Geometry" with metadata skin = "ground" | "ramp"
/// stops rendering (its collision stays) and is covered with a MultiMesh of modelled 4 x 4 m tiles.
/// </summary>
public partial class LevelDresser : Node
{
	[Export] public PackedScene GroundTile { get; set; }
	[Export] public PackedScene RampTile { get; set; }
	[Export] public float TileSize { get; set; } = 4f;

	public override void _Ready()
	{
		var geometry = GetParent().GetNodeOrNull("Geometry");
		if (geometry == null) return;
		foreach (Node child in geometry.GetChildren())
		{
			if (child is not CsgBox3D box || !box.HasMeta("skin")) continue;
			var scene = (string)box.GetMeta("skin") == "ramp" ? RampTile : GroundTile;
			Mesh mesh = ToonModel.ToonMesh(scene);
			if (mesh == null) continue;
			Dress(box, mesh);
		}
	}

	private void Dress(CsgBox3D box, Mesh mesh)
	{
		Vector3 size = box.Size;
		int nx = Mathf.Max(1, Mathf.RoundToInt(size.X / TileSize));
		int nz = Mathf.Max(1, Mathf.RoundToInt(size.Z / TileSize));
		float tw = size.X / nx, tl = size.Z / nz;
		var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = mesh, InstanceCount = nx * nz };
		var basis = Basis.Identity.Scaled(new Vector3(tw / TileSize, size.Y, tl / TileSize));
		int i = 0;
		for (int ix = 0; ix < nx; ix++)
			for (int iz = 0; iz < nz; iz++)
			{
				var pos = new Vector3(-size.X / 2f + (ix + 0.5f) * tw, size.Y / 2f, -size.Z / 2f + (iz + 0.5f) * tl);
				mm.SetInstanceTransform(i++, new Transform3D(basis, pos));
			}
		box.Layers = 0; // stop rendering, collision is unaffected
		box.AddChild(new MultiMeshInstance3D { Multimesh = mm });
	}
}
