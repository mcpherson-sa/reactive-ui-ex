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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ReactiveUI;

namespace ReactiveUIPlay
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class OrderItemControl
    {
        public OrderItemControl()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.Bind(
                    ViewModel,
                    viewModel => viewModel.Product,
                    view => view.Product.Text).DisposeWith(disposables);
            });
        }

    }
}
