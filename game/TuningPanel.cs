using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using NiceTouch.Sim;

namespace NiceTouch;

// A box for every tuning value, built from the sim's Tuning so the two can
// never drift apart. It only edits numbers: Main decides what a change means.
public partial class TuningPanel : PanelContainer
{
    private static readonly PropertyInfo[] Values = typeof(Tuning).GetProperties()
        .Where(p => p.PropertyType == typeof(float) && p.CanWrite)
        .ToArray();

    private readonly Dictionary<PropertyInfo, LineEdit> _boxes = [];
    private Tuning _tuning = new();

    public event Action<Tuning>? Changed;
    public event Action? Closed;

    public override void _Ready()
    {
        Visible = false;
        // Nearly solid, so the numbers read clearly over the tunnel.
        AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.07f, 0.08f, 0.12f, 0.96f) });

        var rows = new GridContainer { Columns = 2 };
        rows.AddThemeConstantOverride("h_separation", 24);
        foreach (PropertyInfo value in Values)
        {
            // "SpinHalfLifeSeconds" reads as "Spin half life seconds".
            string words = Regex.Replace(value.Name, "(?<!^)([A-Z])", " $1");
            rows.AddChild(new Label { Text = words[..1] + words[1..].ToLowerInvariant() });

            var box = new LineEdit { CustomMinimumSize = new Vector2(140, 0) };
            box.TextSubmitted += _ => Commit(value, box);
            box.FocusExited += () => Commit(value, box);
            rows.AddChild(box);
            _boxes[value] = box;
        }

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 12);
        buttons.AddChild(Button("Reset to defaults", () => Apply(new Tuning())));
        buttons.AddChild(Button("Copy values", () => DisplayServer.ClipboardSet(_tuning.ToJson())));
        buttons.AddChild(Button("Close", () => Closed?.Invoke()));

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 12);
        column.AddChild(new Label { Text = "Tuning. Changing a value restarts the rally." });
        column.AddChild(new ScrollContainer { CustomMinimumSize = new Vector2(440, 480), Name = "Scroll" });
        column.GetNode("Scroll").AddChild(rows);
        column.AddChild(buttons);

        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 16);
        margin.AddChild(column);
        AddChild(margin);

        SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;
    }

    public void Open(Tuning tuning)
    {
        _tuning = tuning;
        ShowValues();
        Visible = true;
    }

    public void Close()
    {
        // Letting go of the box being typed in commits what was typed.
        GetViewport().GuiReleaseFocus();
        Visible = false;
    }

    private static Button Button(string text, Action pressed)
    {
        var button = new Button { Text = text };
        button.Pressed += pressed;
        return button;
    }

    private void ShowValues()
    {
        foreach ((PropertyInfo value, LineEdit box) in _boxes)
            box.Text = ((float)value.GetValue(_tuning)!).ToString(CultureInfo.InvariantCulture);
    }

    // Takes what was typed if it is a number that makes sense. Every tuning
    // value is a size, a speed, a share or a time, so none can be negative.
    private void Commit(PropertyInfo value, LineEdit box)
    {
        bool isNumber = float.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float typed);
        if (!isNumber || !float.IsFinite(typed) || typed < 0 || typed == (float)value.GetValue(_tuning)!)
        {
            ShowValues();
            return;
        }

        Tuning changed = _tuning with { };
        value.SetValue(changed, typed);
        Apply(changed);
    }

    private void Apply(Tuning tuning)
    {
        if (tuning == _tuning)
            return;

        _tuning = tuning;
        ShowValues();
        Changed?.Invoke(tuning);
    }
}
