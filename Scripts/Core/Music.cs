using Godot;

namespace PawStorm;

/// <summary>
/// Background music that survives scene changes (a host node under the root) and crossfades between tracks.
/// Tracks are res://Audio/Music/&lt;name&gt;.wav, played as seamless loops on the Music bus.
/// </summary>
public static class Music
{
	private static Node _host;
	private static AudioStreamPlayer _a, _b, _active;
	private static string _current = "";
	private static bool _ducked;

	public static void Play(Node context, string track, float fade = 1.2f)
	{
		if (context == null || string.IsNullOrEmpty(track) || track == _current) return;
		AudioSettings.Init();
		var tree = context.GetTree();
		_current = track;
		// Adding to the root while a scene is still being set up is only safe deferred.
		Callable.From(() => Start(tree, track, fade)).CallDeferred();
	}

	public static void Stop(float fade = 0.8f)
	{
		_current = "";
		if (_active == null || !GodotObject.IsInstanceValid(_active)) return;
		var old = _active;
		var tw = old.CreateTween();
		tw.TweenProperty(old, "volume_db", -50f, fade);
		tw.TweenCallback(Callable.From(old.Stop));
		_active = null;
	}

	/// <summary>Lower the music a little while paused.</summary>
	public static void Duck(bool on)
	{
		_ducked = on;
		if (_active != null && GodotObject.IsInstanceValid(_active))
			_active.CreateTween().TweenProperty(_active, "volume_db", on ? -9f : 0f, 0.25f);
	}

	private static void Start(SceneTree tree, string track, float fade)
	{
		if (track != _current) return; // superseded
		var stream = Sfx.Load($"res://Audio/Music/{track}.wav", loop: true);
		if (stream == null) return;
		if (_host == null || !GodotObject.IsInstanceValid(_host))
		{
			_host = new Node { Name = "MusicHost", ProcessMode = Node.ProcessModeEnum.Always };
			_a = new AudioStreamPlayer { Bus = AudioSettings.MusicBus };
			_b = new AudioStreamPlayer { Bus = AudioSettings.MusicBus };
			_host.AddChild(_a);
			_host.AddChild(_b);
			tree.Root.AddChild(_host);
			_active = null;
		}
		var next = _active == _a ? _b : _a;
		var prev = _active;
		next.Stream = stream;
		next.VolumeDb = -50f;
		next.Play();
		float target = _ducked ? -9f : 0f;
		next.CreateTween().TweenProperty(next, "volume_db", target, fade);
		if (prev != null && prev.Playing)
		{
			var tw = prev.CreateTween();
			tw.TweenProperty(prev, "volume_db", -50f, fade);
			tw.TweenCallback(Callable.From(prev.Stop));
		}
		_active = next;
	}
}
