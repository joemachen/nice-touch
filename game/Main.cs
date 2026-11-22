using Godot;
using NiceTouch.Sim;
using Side = NiceTouch.Sim.Side;

namespace NiceTouch;

// Feeds the sim its input and ticks, and shows what it reports. No game
// rules live here.
public partial class Main : Node3D
{
    private const float FitMargin = 1.1f;

    private readonly RallyView _view = new();
    private readonly Label _debugLine = new();
    private readonly Label _message = new();

    private Tuning _tuning = new();
    private Match _match = null!;
    private Camera3D _camera = null!;
    private bool _serveClicked;

    public override void _Ready()
    {
        // The sim owns the tick rate. Godot's fixed tick follows it.
        Engine.PhysicsTicksPerSecond = Tuning.TicksPerSecond;

        _camera = GetNode<Camera3D>("Camera");
        AddChild(_view);
        BuildHud();

        GetViewport().SizeChanged += FitCameraToTunnelFace;
        Input.MouseMode = Input.MouseModeEnum.ConfinedHidden;
        StartRally();
    }

    // A new rally session: a fresh match waiting for the serve.
    private void StartRally()
    {
        _match = new Match(_tuning);
        _serveClicked = false;
        _view.Reset(_match);
        FitCameraToTunnelFace();
    }

    public override void _PhysicsProcess(double delta)
    {
        // The mouse is read once per sim tick. Its position as a fraction
        // of the window maps onto the tunnel face.
        Vector2 window = GetViewport().GetVisibleRect().Size;
        Vector2 mouse = GetViewport().GetMousePosition() / window;
        var target = new System.Numerics.Vector2(
            (mouse.X - 0.5f) * _tuning.TunnelWidth,
            (0.5f - mouse.Y) * _tuning.TunnelHeight);

        _match.Step(new TickInput(target, _serveClicked));
        _serveClicked = false;
        _view.Take(_match);
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
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            _serveClicked = true;

        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        switch (key.Keycode)
        {
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

    private void BuildHud()
    {
        var hud = new CanvasLayer();
        AddChild(hud);

        Place(hud, _debugLine, Control.LayoutPreset.TopLeft, fontSize: 18);
        // Low on the screen, clear of the far end of the tunnel.
        Place(hud, _message, Control.LayoutPreset.CenterBottom, fontSize: 36);
        _message.GrowHorizontal = Control.GrowDirection.Both;
        _message.GrowVertical = Control.GrowDirection.Begin;
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
