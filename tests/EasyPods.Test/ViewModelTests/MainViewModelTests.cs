using EasyPods.Model.Bluetooth;
using EasyPods.ViewModel.ViewModels;
using Xunit;

namespace EasyPods.Test.ViewModelTests;

public class MainViewModelTests
{
    [Fact]
    public async Task StartMonitoring_UpdatesStatusMessage_OnSuccess()
    {
        using var btService = new WindowsBluetoothService();
        using var vm = new MainViewModel(btService);

        await vm.StartMonitoringCommand.ExecuteAsync(null);

        Assert.Contains("Monitoring for AirPods", vm.StatusMessage);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task StopMonitoring_UpdatesStatusMessage_OnSuccess()
    {
        using var btService = new WindowsBluetoothService();
        using var vm = new MainViewModel(btService);

        await vm.StartMonitoringCommand.ExecuteAsync(null);
        await vm.StopMonitoringCommand.ExecuteAsync(null);

        Assert.Equal("Monitoring paused.", vm.StatusMessage);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void AdvertisementsProcessed_PopulatesAirPodsList_WithSignalStrength()
    {
        using var btService = new WindowsBluetoothService();
        using var vm = new MainViewModel(btService);

        var payload = new byte[27];
        payload[0] = 0x07;
        payload[1] = 0x19;
        payload[2] = 0x0E;
        payload[3] = 0x20;
        payload[5] = 0x99;
        payload[6] = 0xAA;

        btService.ProcessAdvertisement(0x112233445566, "Guy's AirPods Pro", payload, rssi: -55);

        Assert.Single(vm.AirPodsList);
        Assert.Equal("Guy's AirPods Pro", vm.AirPodsList[0].Name);
        Assert.Equal(-55, vm.AirPodsList[0].Rssi);
        Assert.Equal("Excellent (Nearby)", vm.AirPodsList[0].SignalStrength);
        Assert.NotNull(vm.SelectedAirPods);
    }

    [Fact]
    public void RadioDisabled_SetsErrorAndStatusMessage()
    {
        using var btService = new WindowsBluetoothService();
        using var vm = new MainViewModel(btService);

        btService.SetRadioState(false);

        Assert.False(vm.IsBluetoothRadioOn);
        Assert.True(vm.HasError);
        Assert.Contains("turned off", vm.ErrorMessage);
    }
}
