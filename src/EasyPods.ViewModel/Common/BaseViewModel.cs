using CommunityToolkit.Mvvm.ComponentModel;

namespace EasyPods.ViewModel.Common;

/// <summary>
/// Base class for all EasyPods ViewModels providing observable property support and thread-safe dispatching.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    protected void SetError(string? message)
    {
        ErrorMessage = message;
        HasError = !string.IsNullOrWhiteSpace(message);
    }

    protected void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }
}
