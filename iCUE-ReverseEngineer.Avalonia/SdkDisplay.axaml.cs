using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using iCUE_ReverseEngineer.Icue;
using iCUE_ReverseEngineer.Icue.Gsi;
using iCUE_ReverseEngineer.Icue.Sdk;
using JetBrains.Annotations;

namespace iCUE_ReverseEngineer.Avalonia;

public partial class SdkDisplay : UserControl
{
    private readonly Dictionary<string, bool> _states = new();
    private readonly HashSet<string> _events = [];
    private readonly FrozenDictionary<IcueLedId, Border> _keyboardLeds;

    private readonly GameHandler _gameHandler;

    // just for design time
    [UsedImplicitly]
    public SdkDisplay()
    {
        InitializeComponent();

        _keyboardLeds = GenerateKeyboard();
        var keyboardWidth = DevicesPreset.KeyboardLedPositions.Select(l => l.Left + l.Width).Max();
        var keyboardHeight = DevicesPreset.KeyboardLedPositions.Select(l => l.Top + l.Height).Max();
        KeyboardPanel.Width = keyboardWidth;
        KeyboardPanel.Height = keyboardHeight;
    }

    public SdkDisplay(GameHandler gameHandler)
    {
        _gameHandler = gameHandler;

        InitializeComponent();

        _keyboardLeds = GenerateKeyboard();
        var keyboardWidth = DevicesPreset.KeyboardLedPositions.Select(l => l.Left + l.Width).Max();
        var keyboardHeight = DevicesPreset.KeyboardLedPositions.Select(l => l.Top + l.Height).Max();
        KeyboardPanel.Width = keyboardWidth;
        KeyboardPanel.Height = keyboardHeight;

        SdkConnectionTitle.Text = $"SDK Connection, PID: {gameHandler.GamePid}";

        _gameHandler.GsiHandler.StateAdded += GsiHandlerOnStateAdded;
        _gameHandler.GsiHandler.StateRemoved += GsiHandlerOnStateRemoved;
        _gameHandler.GsiHandler.StatesCleared += GsiHandlerOnStatesCleared;
        _gameHandler.GsiHandler.EventAdded += GsiHandlerOnEventAdded;
        _gameHandler.SdkHandler.ColorsUpdated += SdkHandlerOnColorsUpdated;
    }

    public void CollapseKeyboard()
    {
        KeyboardPanel.IsVisible = false;
    }

    private FrozenDictionary<IcueLedId, Border> GenerateKeyboard()
    {
        var leds = DevicesPreset.KeyboardLedPositions;
        var dictionary = new Dictionary<IcueLedId, Border>();

        // clear color boxes to Panel
        foreach (var led in leds)
        {
            var colorBox = new Border
            {
                Background = Brushes.Gray,
                Width = led.Width / 2,
                Height = led.Height / 2,
            };

            // Set the position of the color box
            Canvas.SetTop(colorBox, led.Top);
            Canvas.SetLeft(colorBox, led.Left);

            KeyboardPanel.Children.Add(colorBox);
            dictionary.Add((IcueLedId)led.LedId, colorBox);
        }

        // Return a frozen dictionary for immutability
        return dictionary.ToFrozenDictionary();
    }

    private void UpdateStates()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            StatesList.Children.Clear();
            StatesList.Children.AddRange(
                _states.Select(kvp => new TextBlock
                {
                    Text = kvp.Key,
                    Foreground = kvp.Value ? Brushes.Green : Brushes.Red,
                    Margin = new Thickness(5)
                })
            );
        });
    }

    private void UpdateEvents()
    {
        Dispatcher.UIThread.Post(() =>
        {
            EventsList.Children.Clear();
            foreach (var gameEvent in _events)
            {
                EventsList.Children.Add(new TextBlock
                {
                    Text = gameEvent,
                    Margin = new Thickness(5)
                });
            }
        });
    }

    private void GsiHandlerOnStateAdded(object? sender, IcueStateEventArgs icueStateEventArgs)
    {
        _states[icueStateEventArgs.StateName] = true;

        UpdateStates();
    }

    private void GsiHandlerOnStateRemoved(object? sender, IcueStateEventArgs icueStateEventArgs)
    {
        _states[icueStateEventArgs.StateName] = false;

        UpdateStates();
    }

    private void GsiHandlerOnStatesCleared(object? sender, EventArgs e)
    {
        var stateNames = _states.Keys.ToList();
        foreach (var stateName in stateNames)
        {
            _states[stateName] = false;
        }
        UpdateStates();
    }

    private void GsiHandlerOnEventAdded(object? sender, IcueStateEventArgs icueStateEventArgs)
    {
        _events.Add(icueStateEventArgs.StateName);
        UpdateEvents();
    }

    private void SdkHandlerOnColorsUpdated(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(UpdateKeyboardColors);
    }

    private void UpdateKeyboardColors()
    {
        KeyboardPanel.IsVisible = true;
        var sdkHandlerLedColors = _gameHandler.SdkHandler.LedColors;
        foreach (var (ledId, icueColor) in sdkHandlerLedColors)
        {
            if (!_keyboardLeds.TryGetValue(ledId, out var colorBox)) continue;
            var color = new Color(255, icueColor.R, icueColor.G, icueColor.B);
            // Update the color of the corresponding LED
            colorBox.Background = new SolidColorBrush(color);
        }
    }
}