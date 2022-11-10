using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace ReactiveUIPlay
{
    public class OrderViewModel : ReactiveObject
    {
        public ReactiveCommand<Unit, Unit> ChangeProducts { get; }


        public OrderViewModel()
        {
            //Locator.CurrentMutable.Register(() => new OrderWindow(), typeof(IViewFor<OrderViewModel>));
            //Locator.CurrentMutable.Register(() => new OrderItemControl(), typeof(IViewFor<OrderItemViewModel>));

            ChangeProducts = ReactiveCommand.Create(() =>
            {
                foreach (var item in OrderItems)
                {
                    item.Product = "Trampoline";
                }
            });
        }

        [Reactive]
        public string? Customer { get; set; } = "Davey Jones";

        [Reactive]
        public ObservableCollection<OrderItemViewModel> OrderItems { get; set; } = new ObservableCollection<OrderItemViewModel>()
        {
            new OrderItemViewModel { Product = "Golf Club" },
            new OrderItemViewModel { Product =  "Frisbee" }
        };
    }
}
