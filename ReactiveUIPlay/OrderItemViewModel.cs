using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUIPlay2;

namespace ReactiveUIPlay
{   
    public class OrderItemViewModel : ReactiveObject, IViewModelFor<OrderItem>
    {
        private readonly ObservableAsPropertyHelper<String> _product;

        public OrderItemViewModel()
        {
            _product = this
                .WhenAnyValue(x => x.Model.Product)
                .ToProperty(this, x => x.Product);
        }

        public OrderItem Model { get; } = new();

        //[Reactive]
        //public string? Product { get; set; }

        public string Product
        {
            get => _product.Value;
            set => Model.Product = value;
        }

        public override string ToString()
        {
            return Product ?? "(None)";
        }
    }
}
