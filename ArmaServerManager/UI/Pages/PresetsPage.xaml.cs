using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ArmaServerManager.UI.ViewModels;

namespace ArmaServerManager.UI.Pages;

public sealed partial class PresetsPage : Page
{
    public PresetsViewModel ViewModel { get; }

    public PresetsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<PresetsViewModel>();
        DataContext = ViewModel;
    }
}