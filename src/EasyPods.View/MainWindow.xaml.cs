using System.Windows;
using EasyPods.Model.Bluetooth;
using EasyPods.ViewModel.ViewModels;

namespace EasyPods.View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var bluetoothService = new WindowsBluetoothService();
        _viewModel = new MainViewModel(bluetoothService);
        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Auto-start monitoring when the window opens
        await _viewModel.StartMonitoringCommand.ExecuteAsync(null);
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _viewModel.Dispose();
    }
}