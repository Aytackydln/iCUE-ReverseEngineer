using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using iCUE_ReverseEngineer.Icue;

namespace iCUE_ReverseEngineer.Avalonia;

public partial class MainWindow : Window
{
    private IcueServer? _icueServer;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;
        try
        {
            _icueServer = new IcueServer();
            _icueServer.GameConnected += IcueServerOnGameConnected;
            _icueServer.Run();
        }
        catch (Exception ex)
        {
            TextBlock.Text = "Failed to create replica server\n"+  ex.Message;
        }
    }

    private void IcueServerOnGameConnected(object? sender, GameHandler e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var sdkDisplay = new SdkDisplay(e);
            MainPanel.Children.Add(sdkDisplay);
            
            e.GameDisconnected += eOnGameDisconnected(sdkDisplay);
        });

        EventHandler eOnGameDisconnected(SdkDisplay sdkDisplay)
        {
            return (_, _) =>
            {
                e.GameDisconnected -= eOnGameDisconnected(sdkDisplay);
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    sdkDisplay.CollapseKeyboard();
                });
            };
        }
    }
}