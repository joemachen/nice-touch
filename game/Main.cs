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
    private const string Keys = "F1 tuning    F9 save recording    R new game    Esc quit";
    private const ulong NoticeMsec = 6000;
    private static readonly Color Gold = new(1f, 0.8f, 0.2f);

    private readonly RallyView _view = new();
    private readonly Beeps _beeps = new();
    private readonly Label _debugLine = new();
    private readonly Label _status = new();
    private readonly Label _customTuning = new() { Text = "CUSTOM TUNING" };
    private readonly Label _notice = new() { Text = Keys };
    private readonly TuningPanel _panel = new();

    // The messages low on the screen, from the top down. What a winner
    // paid is a row of cells read left to right as a sum, each a name
    // over its points. A row keeps the block short, so it stays below the
    // far end of the tunnel, where the ball got past.
    private readonly Label _niceTouch = new() { Text = "NICE TOUCH" };
    private readonly Label _headline = new();
    private readonly Label _summary = new();
    private readonly Label[] _pointCells = [new(), new(), new(), new(), new(), new(), new(), new()];
    private readonly Label _prompt = new();

    // Every input since this game began, for the recorder.
    private readonly List<TickInput> _inputs = [];

    private Tuning _tuning = new();
    private Game _game = null!;
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
        StartGame();
    }

    // A new game: level 1, waiting for the serve.
    private void StartGame()
    {
        _game = new Game(_tuning);
        _inputs.Clear();
        _serveClicked = false;
        _view.Reset(_game.Match);
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
        _game.Step(input);

        // A recording is of one game, so a new game starts it again.
        if (_game.Events.HasFlag(TickEvents.NewGame))
            _inputs.Clear();

        _view.Take(_game.Match);
        _beeps.Play(_game.Events);
    }

    public override void _Process(double delta)
    {
        Match match = _game.Match;
        Winner? winner = _game.Winner;
        bool ended = _game.Phase is GamePhase.GameOver or GamePhase.Won;

        _debugLine.Text =
            $"spin: side {match.BallSpin.Y:0.0}, top {match.BallSpin.X:0.0}    " +
            $"pace: {Mathf.Abs(match.BallVelocity.Z):0.0}    rally: {match.RallyLength}";

        _status.Text =
            $"SCORE {_game.Score}      LEVEL {_game.Level} of {_tuning.Levels}      " +
            $"LIVES {_game.PlayerLives}      OPPONENT LIVES {_game.OpponentLives}";

        Say(_headline, _game.Phase switch
        {
            GamePhase.GameOver => "GAME OVER",
            GamePhase.Won => "YOU WIN",
            GamePhase.LevelClear => $"LEVEL {_game.Level} CLEAR",
            _ when match.Phase == Phase.Rally => "",
            // No point has been played on this level yet.
            _ when match.LastMiss == null => $"LEVEL {_game.Level}",
            _ when match.LastMiss == Side.Player => "YOU MISSED",
            _ => "OPPONENT MISSED",
        });

        Say(_summary, _game.Phase switch
        {
            GamePhase.GameOver => $"Score {_game.Score}    Reached level {_game.Level}",
            GamePhase.Won => $"Final score {_game.Score}",
            _ => "",
        });

        _niceTouch.Visible = winner is { NiceTouch: true };
        ShowPoints(winner);

        // In both cases the click will do something only once the pause
        // after the last miss is over.
        Say(_prompt, match.Phase != Phase.Serving ? "" : ended ? "Click to play again" : "Click to serve");

        if (Time.GetTicksMsec() > _noticeUntilMsec)
            _notice.Text = Keys;
    }

    // What the latest winner paid, cell by cell, while it is on show.
    private void ShowPoints(Winner? winner)
    {
        var cells = new List<string>();
        if (winner != null)
        {
            cells.Add($"Winner\n{winner.BasePoints}");
            cells.Add($"Rally of {winner.RallyHits}\n+ {winner.RallyPoints}");
            cells.Add($"Bend {winner.Bend:0.0}\n+ {winner.BendPoints}");
            cells.Add($"Kick x{winner.Kicks}\n+ {winner.KickPoints}");
            if (winner.NiceTouch)
                cells.Add($"Style doubled\n+ {winner.NiceTouchPoints}");
            cells.Add($"Level {winner.Level}\nx {winner.Level}");
            cells.Add($"Total\n= {winner.Total}");
            if (winner.LevelBonus > 0)
                cells.Add($"Level clear bonus\n+ {winner.LevelBonus}");
        }

        for (int i = 0; i < _pointCells.Length; i++)
            Say(_pointCells[i], i < cells.Count ? cells[i] : "");
    }

    // An empty message takes up no room.
    private static void Say(Label label, string text)
    {
        label.Text = text;
        label.Visible = text != "";
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
                StartGame();
                break;
            case Key.Escape:
                GetTree().Quit();
                break;
        }
    }

    // The boot smoke test in test.bat reads this line.
    public override void _ExitTree() => GD.Print($"sim ticks at exit: {_game.Tick}");

    // Writes the game so far to the recordings folder beside run.bat and
    // says where it went.
    private void SaveRecording()
    {
        string name = $"game-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        try
        {
            string folder = Path.Combine(RepoFolder, "recordings");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name), Recording.Save(_game, _inputs));
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

    // Changing any value restarts the game, and is remembered for next time.
    private void OnTuningChanged(Tuning tuning)
    {
        _tuning = tuning;
        StartGame();
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

        Place(hud, Style(_debugLine, fontSize: 18), Control.LayoutPreset.TopLeft);
        Place(hud, Style(_customTuning, fontSize: 18, Gold), Control.LayoutPreset.TopRight);
        Place(hud, Style(_notice, fontSize: 16), Control.LayoutPreset.BottomLeft);

        // Under the debug line, so the two never run into each other.
        Place(hud, Style(_status, fontSize: 26), Control.LayoutPreset.CenterTop, margin: 48);
        _status.GrowHorizontal = Control.GrowDirection.Both;

        var points = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        points.AddThemeConstantOverride("separation", 36);
        foreach (Label cell in _pointCells)
            points.AddChild(Centred(Style(cell, fontSize: 22)));

        // Low on the screen, growing upwards as lines are added.
        var messages = new VBoxContainer();
        messages.AddChild(Centred(Style(_niceTouch, fontSize: 52, Gold)));
        messages.AddChild(Centred(Style(_headline, fontSize: 36)));
        messages.AddChild(Centred(Style(_summary, fontSize: 26)));
        messages.AddChild(points);
        messages.AddChild(Centred(Style(_prompt, fontSize: 26)));
        Place(hud, messages, Control.LayoutPreset.CenterBottom, margin: 40);
        messages.GrowHorizontal = Control.GrowDirection.Both;
        messages.GrowVertical = Control.GrowDirection.Begin;

        hud.AddChild(_panel);
    }

    // Outlined, so the words read over the lines of the tunnel.
    private static Label Style(Label label, int fontSize, Color? colour = null)
    {
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeConstantOverride("outline_size", fontSize / 4);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        if (colour is Color tint)
            label.AddThemeColorOverride("font_color", tint);
        return label;
    }

    private static Label Centred(Label label)
    {
        label.HorizontalAlignment = HorizontalAlignment.Center;
        return label;
    }

    private static void Place(CanvasLayer hud, Control control, Control.LayoutPreset where, int margin = 14)
    {
        control.MouseFilter = Control.MouseFilterEnum.Ignore;
        hud.AddChild(control);
        control.SetAnchorsAndOffsetsPreset(where, Control.LayoutPresetMode.Minsize, margin);
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
