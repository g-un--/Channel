using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace Channel.Extensions
{
    public static class ObserverInterceptExtensions
    {
        public static Request<T> InterceptOnCompletedWith<T>(this Request<T> observer, Action onCompleted)
        {
            return observer.InterceptOnCompletedWith(onCompleted, Scheduler.Immediate);
        }

        public static Request<T> InterceptOnCompletedWith<T>(this Request<T> observer, Action onCompleted, IScheduler scheduler)
        {
            return new ObserverOnCompletedInterceptor<T>(observer, onCompleted, scheduler);
        }
    }
}
