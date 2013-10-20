using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using Channel.Helpers;
using Channel.Extensions;

namespace Channel
{
    public class Balancer<T>
    {
        private readonly RequestChannel<T> requester;
        private readonly Subject<Worker<T>> workersQueue;
        private readonly IObservable<Worker<T>> availableWorkers;
        private readonly IObservable<Timestamped<WorkerContract<T>>> workerContracts;
        private readonly TimeSpan policy;
        private readonly IScheduler balancerScheduler;

        public Balancer(RequestChannel<T> requester, IObservable<Worker<T>> workers, TimeSpan policy, IScheduler scheduler)
        {
            if (requester == null)
            {
                throw new ArgumentNullException("requester");
            }
            if (workers == null)
            {
                throw new ArgumentNullException("availableWorkers");
            }
            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            this.requester = requester;
            this.balancerScheduler = scheduler;
            this.availableWorkers = workers;
            this.workersQueue = new Subject<Worker<T>>();
            this.workerContracts = workersQueue.Zip(requester, (worker, request) =>
                {
                    var interceptor = request.InterceptOnCompletedWith(() => workersQueue.OnNext(worker), balancerScheduler);
                    var contractPolicy = worker.Subscribe(interceptor);
                    return new WorkerContract<T>(worker, contractPolicy);
                }).Timestamp();
            this.policy = policy;
        }

        public IDisposable Connect()
        {
            var disposables = new CompositeDisposable();

            var workersDisposable = this.workerContracts.SubscribeOn(balancerScheduler).Subscribe(workerContract =>
            {
                var scheduledDisposable = balancerScheduler.Schedule(policy, () => { workerContract.Value.Policy.Dispose(); workersQueue.OnNext(workerContract.Value.Worker); });
                disposables.Add(scheduledDisposable);
            });
            disposables.Add(workersDisposable);

            var startDisposable = availableWorkers.Concat(Observable.Never<Worker<T>>()).SubscribeOn(balancerScheduler).Subscribe(workersQueue);
            disposables.Add(startDisposable);

            return disposables;
        }
    }
}
