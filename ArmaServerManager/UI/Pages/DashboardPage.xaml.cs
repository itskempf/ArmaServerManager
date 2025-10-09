using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ArmaServerManager.UI.ViewModels;

namespace ArmaServerManager.UI.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
        this.InitializeComponent();
        this.DataContext = ViewModel;
    }
}