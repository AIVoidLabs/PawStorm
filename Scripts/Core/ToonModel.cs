using System.Collections.Generic;
using Godot;

namespace PawStorm;

/// <summary>
/// Put on an imported .glb instance: gives every mesh toon shading and the cartoon outline
/// (inverted-hull overlay), without touching the .glb itself.
/// </summary>
public partial class ToonModel : Node3D
{
	[Export] public bool Outline { get; set; } = true;

	private static readonly Dictionary<Material, Material> ToonCache = new();
	private static Material _outline;

	public override void _Ready()
	{
		_outline ??= GD.Load<Material>("res://Art/Materials/Outline.tres");
		Apply(this);
	}

	private void Apply(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D mi && mi.Mesh != null)
			{
				if (Outline) mi.MaterialOverlay = _outline;
				for (int s = 0; s < mi.Mesh.GetSurfaceCount(); s++)
				{
					if (mi.Mesh.SurfaceGetMaterial(s) is not BaseMaterial3D src) continue;
					if (!ToonCache.TryGetValue(src, out var toon))
					{
						var d = (BaseMaterial3D)src.Duplicate();
						d.DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Toon;
						d.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
						ToonCache[src] = toon = d;
					}
					mi.SetSurfaceOverrideMaterial(s, toon);
				}
			}
			Apply(child);
		}
	}

	/// <summary>All mesh instances below <paramref name="root"/> (recursive).</summary>
	public static IEnumerable<MeshInstance3D> Meshes(Node root)
	{
		foreach (Node c in root.GetChildren())
		{
			if (c is MeshInstance3D m) yield return m;
			foreach (var sub in Meshes(c)) yield return sub;
		}
	}
}
