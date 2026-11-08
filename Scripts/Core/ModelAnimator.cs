using Godot;

namespace PawStorm;

/// <summary>Thin wrapper over the AnimationPlayer that comes with an imported .glb (clips baked by make_models.py).</summary>
public sealed class ModelAnimator
{
	private readonly AnimationPlayer _player;
	private string _current;

	public ModelAnimator(Node modelRoot)
		=> _player = modelRoot?.FindChild("AnimationPlayer", true, false) as AnimationPlayer;

	public bool Available => _player != null;

	/// <summary>Play a clip (no-op if it is already playing) and set its speed.</summary>
	public void Play(string clip, float speed = 1f, float blend = 0.12f)
	{
		if (_player == null || !_player.HasAnimation(clip)) return;
		if (_current != clip)
		{
			_player.Play(clip, blend);
			_current = clip;
		}
		_player.SpeedScale = speed;
	}

	/// <summary>Start a clip at a random point (so many copies do not move in sync).</summary>
	public void PlayRandomOffset(string clip, float speed = 1f)
	{
		if (_player == null || !_player.HasAnimation(clip)) return;
		Play(clip, speed, 0f);
		_player.Seek(GD.Randf() * _player.GetAnimation(clip).Length, true);
	}
}
