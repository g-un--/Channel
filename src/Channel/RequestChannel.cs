using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channel
{
    public interface RequestChannel<in T> : IObservable<Request<T>>
    {
    }
}
