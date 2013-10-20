using Microsoft.Reactive.Testing;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Channel.Tests
{
    public class LearnTestSchedulerTests
    {
        [Fact]
        public void TestSchedulerAdvanceByOneTickExecutesAllActionsRegisteredWithoutTimespan()
        {
            bool firstActionExecuted = false;
            bool secondActionExecuted = false;
            var testScheduler = new TestScheduler();
            var scheduler = testScheduler as IScheduler;

            scheduler.Schedule(() => firstActionExecuted = true);
            scheduler.Schedule(() => secondActionExecuted = true);

            testScheduler.AdvanceBy(1);

            Assert.True(firstActionExecuted);
            Assert.True(secondActionExecuted);
        }

        [Fact]
        public void TestSchedulerWillExecuteActionIfAdvancedWithTimespan()
        {
            bool actionExecuted = false;
            var testScheduler = new TestScheduler();
            var scheduler = testScheduler as IScheduler;

            scheduler.Schedule(TimeSpan.FromDays(1), () => actionExecuted = true);
            testScheduler.AdvanceTo(TimeSpan.FromDays(1).Ticks);

            Assert.True(actionExecuted);
        }

        [Fact]
        public void EachObservableOnNextTakesOneTickOnTestScheduler()
        {
            var numberOfActions = 10;
            var numberOfObservedActions = 0;
            var testScheduler = new TestScheduler();
            var scheduler = testScheduler as IScheduler;
            var observable = Observable.Create<int>(subscriber =>
                {
                    foreach (var actionIndex in Enumerable.Range(0, numberOfActions))
                    {
                        subscriber.OnNext(actionIndex);
                    }

                    return Disposable.Empty;
                });
            var observer = new Mock<IObserver<int>>();
            observer.Setup(o => o.OnNext(It.IsAny<int>())).Callback(() => { numberOfObservedActions += 1; });

            observable.ObserveOn(scheduler).Subscribe(observer.Object);
            
            testScheduler.AdvanceBy(9);
            Assert.Equal(numberOfObservedActions, 9);

            testScheduler.AdvanceBy(1);
            Assert.Equal(numberOfObservedActions, 10);
        }
    }
}
