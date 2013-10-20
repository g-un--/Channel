using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Channel.Tests
{
    public class LearnObservableTests
    {
        [Fact]
        public void ObservableEmptyShouldCallCompleteOnObserver()
        {
            var onCompletedWasCalled = false;
            var observer = new Mock<IObserver<object>>();
            observer.Setup(o => o.OnCompleted()).Callback(() => { onCompletedWasCalled = true; });

            Observable.Empty<object>().Subscribe(observer.Object);

            Assert.True(onCompletedWasCalled);
        }

        [Fact]
        public void ObservableReturnShouldCallOnNextAndOnComplete()
        {
            var onNextWasCalled = false;
            var onCompletedWasCalled = false;
            var observer = new Mock<IObserver<object>>();
            observer.Setup(o => o.OnCompleted()).Callback(() => { onCompletedWasCalled = true; });
            observer.Setup(o => o.OnNext(It.IsAny<object>())).Callback(() => { onNextWasCalled = true; });

            Observable.Return<object>(new { }).Subscribe(observer.Object);

            Assert.True(onNextWasCalled);
            Assert.True(onCompletedWasCalled);
        }

        [Fact]
        public void ObservableUsingDisposesResourcesWhenInnerObservableCompletes()
        {
            var wasDisposed = false;
            var disposable = Disposable.Create(() => wasDisposed = true);

            Observable.Using(() => disposable, _ => Observable.Empty<object>()).Subscribe(_ => { });

            Assert.True(wasDisposed);
        }

        [Fact]
        public void ObservableSubscribeCanThrowException()
        {
            var observer = new Mock<IObserver<object>>();
            var observable = new Mock<IObservable<object>>();

            observable.Setup(o => o.Subscribe(observer.Object))
                .Returns<IObserver<object>>(subscriber =>
                    {
                        throw new Exception();
                    });

            Assert.Throws<Exception>(() => { observable.Object.Subscribe(observer.Object); });
        }

        [Fact]
        public void ObservableSubscribeSafeRedirectExceptionToOnError()
        {
            var observer = new Mock<IObserver<object>>();
            var observable = new Mock<IObservable<object>>();

            observable.Setup(o => o.Subscribe(observer.Object))
                .Returns<IObserver<object>>(subscriber =>
                {
                    throw new Exception();
                });

            observable.Object.SubscribeSafe(observer.Object);

            observer.Verify(o => o.OnError(It.IsAny<Exception>()));
        }

        [Fact]
        public void ObservablePublishShareTheDataToAllSubscribers()
        {
            object firstObserverObject = true;
            object secondObserverObject = false;
            var firstObserver = new Mock<IObserver<object>>();
            firstObserver.Setup(o => o.OnNext(It.IsAny<object>())).Callback<object>(value => { firstObserverObject = value; });
            var secondObserver = new Mock<IObserver<object>>();
            secondObserver.Setup(o => o.OnNext(It.IsAny<object>())).Callback<object>(value => { secondObserverObject = value; });
            var observable = new Mock<IObservable<object>>();
            observable.Setup(o => o.Subscribe(It.IsAny<IObserver<object>>()))
               .Returns<IObserver<object>>(subscriber =>
               {
                   subscriber.OnNext(new { });
                   subscriber.OnCompleted();

                   return Disposable.Empty;
               });
            var published = observable.Object.Publish();

            published.Subscribe(firstObserver.Object);
            published.Subscribe(secondObserver.Object);
            published.Connect();

            Assert.Equal(firstObserverObject, secondObserverObject);
        }
    }
}
