using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveUIPlay2
{
    public interface IViewModelFor<out T>
        where T : class
    {
        T? Model { get; }
    }
}
