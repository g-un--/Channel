using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channel
{
    public interface Request<in T> : IObserver<T>
    {
    }
}
