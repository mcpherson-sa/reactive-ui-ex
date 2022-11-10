using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ReactiveUIPlay2
{
    public class OrderItem : ReactiveObject
    {
        [Reactive]
        public string Product { get; set; }
    }
}
