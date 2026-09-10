using EasyPods.Model.Bluetooth;
using EasyPods.ViewModel.ViewModels;
using Xunit;

namespace EasyPods.Test.ViewModelTests;

public class MainViewModelTests
{
    [Fact]
    public async Task StartMonitoring_UpdatesStatusMessage_OnSuccess()
    {
        var btService = new WindowsBluetoothService();
        var vm = new MainViewModel(btService);

        await vm.StartMonitoringCommand.ExecuteAsync(null);

        Assert.Contains("Monitoring for AirPods", vm.StatusMessage);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void AdvertisementsProcessed_PopulatesAirPodsList()
    {
        var btService = new WindowsBluetoothService();
        var vm = new MainViewModel(btService);

        var payload = new byte[27];
        payload[0] = 0x07;
        payload[1] = 0x19;
        payload[2] = 0x0E;
        payload[3] = 0x20;
        payload[5] = 0x99;
        payload[6] = 0xAA;

        btService.ProcessAdvertisement(0x112233445566, "Guy's AirPods Pro", payload);

        Assert.Single(vm.AirPodsList);
        Assert.Equal("Guy's AirPods Pro", vm.AirPodsList[0].Name);
        Assert.NotNull(vm.SelectedAirPods);
    }
}
