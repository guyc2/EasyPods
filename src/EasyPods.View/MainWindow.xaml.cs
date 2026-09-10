using System.Windows;
using EasyPods.Model.Bluetooth;
using EasyPods.ViewModel.ViewModels;

namespace EasyPods.View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Initial setup: In Sprint 4 this is wired via DI (IServiceProvider)
        var bluetoothService = new WindowsBluetoothService();
        DataContext = new MainViewModel(bluetoothService);
    }
}