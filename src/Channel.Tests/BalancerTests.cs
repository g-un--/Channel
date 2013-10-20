using Channel.Tests.TestHelpers;
using Microsoft.Reactive.Testing;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Channel.Tests
{
    public class BalancerTests
    {
        Mock<Request<Message>> requestObserver;
        Mock<Worker<Message>> worker;
        Mock<RequestChannel<Message>> requestChannel;

        public BalancerTests()
        {
            requestObserver = new Mock<Request<Message>>();
            worker = new Mock<Worker<Message>>();
            requestChannel = new Mock<RequestChannel<Message>>();
        }

        [Fact]
        public void BalancerConnectShouldTriggerWorkerSubscribeRequest()
        {
            bool workerWasCalled = false;
            var scheduler = new TestScheduler();
            requestObserver.Setup(observer => observer.OnCompleted()).Callback(() => workerWasCalled = true);
            worker.Setup(w => w.Subscribe(It.IsAny<Request<Message>>())).Returns<Request<Message>>(request => { request.OnCompleted(); return Disposable.Empty; });
            requestChannel.Setup(c => c.Subscribe(It.IsAny<IObserver<Request<Message>>>())).Returns<IObserver<Request<Message>>>(observer =>
                {
                    observer.OnNext(requestObserver.Object);
                    return Disposable.Empty;
                });
            var balancer = new Balancer<Message>(requestChannel.Object, Observable.ToObservable(new[] { worker.Object }), TimeSpan.FromSeconds(1), scheduler);

            balancer.Connect();
            scheduler.AdvanceBy(1);

            Assert.True(workerWasCalled);
        }

        [Fact]
        public void BalancerCanReuseAWorkerForMultipleRequestsIfWorkerCompletesHisContract()
        {
            var numberOfRequest = 10000;
            int numberOfTimesWorkerWorkerCompleted = 0;
            var scheduler = new TestScheduler();
            requestObserver.Setup(observer => observer.OnCompleted()).Callback(() => numberOfTimesWorkerWorkerCompleted += 1);
            worker.Setup(w => w.Subscribe(It.IsAny<Request<Message>>())).Returns<Request<Message>>(request => 
            { 
                request.OnCompleted(); 
                return Disposable.Empty; 
            });
            requestChannel.Setup(c => c.Subscribe(It.IsAny<IObserver<Request<Message>>>())).Returns<IObserver<Request<Message>>>(observer =>
            {
                foreach (var index in Enumerable.Range(0, numberOfRequest))
                {
                    observer.OnNext(requestObserver.Object);
                }
                return Disposable.Empty;
            });
            var balancer = new Balancer<Message>(requestChannel.Object, Observable.ToObservable(new[] { worker.Object }), TimeSpan.FromSeconds(20), scheduler);

            balancer.Connect();
            scheduler.AdvanceBy(numberOfRequest);

            Assert.Equal(numberOfRequest, numberOfTimesWorkerWorkerCompleted);
        }

        [Fact]
        public void BalancerCanUseAllAvailableWorkersForMultipleRequests()
        {
            bool workerWasCalled = false;
            bool anotherWorkerWasCalled = false;
            Mock<Worker<Message>> anotherWorker = new Mock<Worker<Message>>();
            var testScheduler = new TestScheduler();
            var scheduler = testScheduler as IScheduler;
            anotherWorker.Setup(w => w.Subscribe(It.IsAny<Request<Message>>())).Returns<Request<Message>>(request =>
            {
                anotherWorkerWasCalled = true;
                request.OnCompleted();
                return Disposable.Empty;
            });
            worker.Setup(w => w.Subscribe(It.IsAny<Request<Message>>())).Returns<Request<Message>>(request =>
            {
                workerWasCalled = true;
                request.OnCompleted();
                return Disposable.Empty;
            });
            requestChannel.Setup(c => c.Subscribe(It.IsAny<IObserver<Request<Message>>>())).Returns<IObserver<Request<Message>>>(observer =>
            {
                foreach (var index in Enumerable.Range(0, 2))
                {
                    observer.OnNext(requestObserver.Object);
                }
                return Disposable.Empty;
            });
            var balancer = new Balancer<Message>(requestChannel.Object, Observable.ToObservable(new[] { worker.Object, anotherWorker.Object }), TimeSpan.FromSeconds(1), scheduler);

            balancer.Connect();
            testScheduler.AdvanceBy(1);

            Assert.True(workerWasCalled);
            Assert.True(anotherWorkerWasCalled);
        }

        [Fact]
        public void BalancerReleaseWorkerAfterPolicyExpires()
        {
            bool workerWasDisposed = false;
            var policy = TimeSpan.FromSeconds(10);
            var scheduler = new TestScheduler();
            worker.Setup(w => w.Subscribe(It.IsAny<Request<Message>>())).Returns<Request<Message>>(request =>
            {
                return Disposable.Create(() => workerWasDisposed = true);
            });
            requestChannel.Setup(c => c.Subscribe(It.IsAny<IObserver<Request<Message>>>())).Returns<IObserver<Request<Message>>>(observer =>
            {
                observer.OnNext(requestObserver.Object);
                return Disposable.Empty;
            });
            var balancer = new Balancer<Message>(requestChannel.Object, Observable.ToObservable(new[] { worker.Object }), policy, scheduler);

            balancer.Connect();
            scheduler.AdvanceTo(policy.Ticks + 1);

            Assert.True(workerWasDisposed);
        }
    }
}
