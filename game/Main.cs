using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using NiceTouch.Sim;
using Side = NiceTouch.Sim.Side;

namespace NiceTouch;

// Feeds the sim its input and ticks, and shows what it reports. No game
// rules live here.
public partial class Main : Node3D
{
    private const float FitMargin = 1.1f;
    private const string Keys = "F1 tuning    F9 save recording    R restart    Esc quit";
    private const ulong NoticeMsec = 6000;

    private readonly RallyView _view = new();
    private readonly Beeps _beeps = new();
    private readonly Label _debugLine = new();
    private readonly Label _message = new();
    private readonly Label _customTuning = new() { Text = "CUSTOM TUNING" };
    private readonly Label _notice = new() { Text = Keys };
    private readonly TuningPanel _panel = new();

    // Every input since this rally session began, for the recorder.
    private readonly List<TickInput> _inputs = [];

    private Tuning _tuning = new();
    private Match _match = null!;
    private Camera3D _camera = null!;
    private bool _serveClicked;
    private ulong _noticeUntilMsec;

    public override void _Ready()
    {
        // The sim owns the tick rate. Godot's fixed tick follows it.
        Engine.PhysicsTicksPerSecond = Tuning.TicksPerSecond;

        _camera = GetNode<Camera3D>("Camera");
        AddChild(_view);
        AddChild(_beeps);
        BuildHud();
        _panel.Changed += OnTuningChanged;
        _panel.Closed += CloseTuningPanel;

        GetViewport().SizeChanged += FitCameraToTunnelFace;
        Input.MouseMode = Input.MouseModeEnum.ConfinedHidden;
        _tuning = LoadRememberedTuning();
        StartRally();
    }

    // A new rally session: a fresh match waiting for the serve.
    private void StartRally()
    {
        _match = new Match(_tuning);
        _inputs.Clear();
        _serveClicked = false;
        _view.Reset(_match);
        FitCameraToTunnelFace();

        // So that values left over from an earlier sitting cannot pass
        // unnoticed.
        _customTuning.Visible = _tuning != new Tuning();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Play is paused while the tuning panel is open.
        if (_panel.Visible)
            return;

        // The mouse is read once per sim tick. Its position as a fraction
        // of the window maps onto the tunnel face.
        Vector2 window = GetViewport().GetVisibleRect().Size;
        Vector2 mouse = GetViewport().GetMousePosition() / window;
        var target = new System.Numerics.Vector2(
            (mouse.X - 0.5f) * _tuning.TunnelWidth,
            (0.5f - mouse.Y) * _tuning.TunnelHeight);

        var input = new TickInput(target, _serveClicked);
        _serveClicked = false;
        _inputs.Add(input);
        _match.Step(input);
        _view.Take(_match);
        _beeps.Play(_match.Events);
    }

    public override void _Process(double delta)
    {
        _debugLine.Text =
            $"spin: side {_match.BallSpin.Y:0.0}, top {_match.BallSpin.X:0.0}    " +
            $"pace: {Mathf.Abs(_match.BallVelocity.Z):0.0}    rally: {_match.RallyLength}";

        _message.Text = _match.Phase switch
        {
            Phase.PointOver when _match.LastMiss == Side.Player => "YOU MISSED",
            Phase.PointOver => "OPPONENT MISSED",
            Phase.Serving => "Click to serve",
            _ => "",
        };

        if (Time.GetTicksMsec() > _noticeUntilMsec)
            _notice.Text = Keys;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && !_panel.Visible)
            _serveClicked = true;

        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        switch (key.Keycode)
        {
            case Key.F1 when _panel.Visible:
                CloseTuningPanel();
                break;
            case Key.F1:
                _view.Hold();
                _panel.Open(_tuning);
                Input.MouseMode = Input.MouseModeEnum.Visible;
                break;
            case Key.F9:
                SaveRecording();
                break;
            case Key.R:
                StartRally();
                break;
            case Key.Escape:
                GetTree().Quit();
                break;
        }
    }

    // The boot smoke test in test.bat reads this line.
    public override void _ExitTree() => GD.Print($"sim ticks at exit: {_match.Tick}");

    // Writes the session so far to the recordings folder beside run.bat and
    // says where it went.
    private void SaveRecording()
    {
        string name = $"rally-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        try
        {
            string folder = Path.Combine(RepoFolder, "recordings");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name), Recording.Save(_match, _inputs));
            _notice.Text = $"Saved recordings/{name}";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _notice.Text = $"The recording could not be saved: {e.Message}";
        }
        _noticeUntilMsec = Time.GetTicksMsec() + NoticeMsec;
    }

    private void CloseTuningPanel()
    {
        _panel.Close();
        _serveClicked = false;
        Input.MouseMode = Input.MouseModeEnum.ConfinedHidden;
    }

    // Changing any value restarts the rally, and is remembered for next time.
    private void OnTuningChanged(Tuning tuning)
    {
        _tuning = tuning;
        StartRally();
        try
        {
            File.WriteAllText(RememberedTuningPath, tuning.ToJson());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"The tuning values could not be remembered: {e.Message}");
        }
    }

    // The values from the last sitting, or the defaults if there are none.
    private static Tuning LoadRememberedTuning()
    {
        try
        {
            return File.Exists(RememberedTuningPath) ? Tuning.FromJson(File.ReadAllText(RememberedTuningPath)) : new Tuning();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            GD.PushWarning($"The remembered tuning values could not be read, so the defaults are in use: {e.Message}");
            return new Tuning();
        }
    }

    // The game runs from source, so its own files sit beside run.bat, one
    // folder above the Godot project. Git ignores them.
    private static string RepoFolder => Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), ".."));
    private static string RememberedTuningPath => Path.Combine(RepoFolder, "tuning.local.json");

    private void BuildHud()
    {
        var hud = new CanvasLayer();
        AddChild(hud);

        Place(hud, _debugLine, Control.LayoutPreset.TopLeft, fontSize: 18);
        // Low on the screen, clear of the far end of the tunnel.
        Place(hud, _message, Control.LayoutPreset.CenterBottom, fontSize: 36);
        _message.GrowHorizontal = Control.GrowDirection.Both;
        _message.GrowVertical = Control.GrowDirection.Begin;
        Place(hud, _customTuning, Control.LayoutPreset.TopRight, fontSize: 18);
        _customTuning.Modulate = new Color(1f, 0.8f, 0.2f);
        Place(hud, _notice, Control.LayoutPreset.BottomLeft, fontSize: 16);

        hud.AddChild(_panel);
    }

    private static void Place(CanvasLayer hud, Label label, Control.LayoutPreset where, int fontSize)
    {
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        hud.AddChild(label);
        label.SetAnchorsAndOffsetsPreset(where, Control.LayoutPresetMode.Minsize, 14);
    }

    // Pulls the camera back until the whole near face of the tunnel fits,
    // whatever shape the window is.
    private void FitCameraToTunnelFace()
    {
        Vector2 size = GetViewport().GetVisibleRect().Size;
        if (size.X <= 0 || size.Y <= 0)
            return;

        float tanY = Mathf.Tan(Mathf.DegToRad(_camera.Fov) / 2);
        float tanX = tanY * size.X / size.Y;
        float distance = Mathf.Max(_tuning.TunnelHeight / 2 / tanY, _tuning.TunnelWidth / 2 / tanX);
        _camera.Position = new Vector3(0, 0, distance * FitMargin);
    }
}
