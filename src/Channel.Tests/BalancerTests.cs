using Channel.Tests.TestHelpers;
using Microsoft.Reactive.Testing;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
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
            var numberOfRequest = 10;
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
            var balancer = new Balancer<Message>(requestChannel.Object, Observable.ToObservable(new[] { worker.Object }), TimeSpan.FromSeconds(1), scheduler);

            balancer.Connect();
            scheduler.AdvanceBy(1);

            Assert.Equal(numberOfRequest, numberOfTimesWorkerWorkerCompleted);
        }
    }
}
