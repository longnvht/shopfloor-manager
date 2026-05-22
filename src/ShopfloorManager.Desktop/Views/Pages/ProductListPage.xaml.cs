using System.Windows.Controls;
using System.Windows.Input;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.ViewModels;

namespace ShopfloorManager.Desktop.Views.Pages;

public partial class ProductListPage : UserControl
{
    public ProductListPage()
    {
        InitializeComponent();
    }

    private void OnCardTapped(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm) return;
        if (sender is System.Windows.FrameworkElement fe && fe.DataContext is ProductWithSessionDto product)
        {
            if (!product.IsAvailable) return; // Chỉ cho chọn sản phẩm Available
            vm.SelectedProduct = product;
        }
    }
}
