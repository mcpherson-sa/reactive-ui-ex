using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ReactiveUIPlay
{   
    /// <summary>
    /// Interaction logic for OrderWindow.xaml
    /// </summary>
    public partial class OrderWindow
    {
        public OrderWindow()    
        {
            InitializeComponent();

            ViewModel = new OrderViewModel();

            this.WhenActivated(disposables =>
            {
                this.Bind(
                    ViewModel,
                    viewModel => viewModel.Customer,
                    view => view.Customer.Text).DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.OrderItems,
                    view => view.OrderItems.ItemsSource).DisposeWith(disposables);

                this.BindCommand(
                    ViewModel,
                    viewModel => viewModel.ChangeProducts,
                    view => view.ChangeProducts);
            });
        }
    }
}
