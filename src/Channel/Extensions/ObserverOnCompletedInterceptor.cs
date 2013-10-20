using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace Channel.Extensions
{
    class ObserverOnCompletedInterceptor<T> : Request<T>
    {
        private readonly IObserver<T> observer;
        private readonly Action onCompleted;
        private readonly IScheduler scheduler;

        public ObserverOnCompletedInterceptor(IObserver<T> observer, Action onCompleted, IScheduler scheduler)
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer");
            }
            if (onCompleted == null)
            {
                throw new ArgumentNullException("onCompleted");
            }
            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            this.observer = observer;
            this.onCompleted = onCompleted;
            this.scheduler = scheduler;
        }

        public void OnCompleted()
        {
            scheduler.Schedule(onCompleted);
            observer.OnCompleted();
        }

        public void OnError(Exception error)
        {
            observer.OnError(error);
        }

        public void OnNext(T value)
        {
            observer.OnNext(value);
        }
    }
}
