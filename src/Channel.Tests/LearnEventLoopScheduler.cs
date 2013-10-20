using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Reactive.Concurrency;
using System.Threading;

namespace Channel.Tests
{
    public class LearnEventLoopScheduler
    {
        [Fact]
        public async Task EventLoopSchedulerShouldScheduleOnTheConstructorThread()
        {
            var actualThreadId = int.MaxValue;
            var expectedThreadId = int.MinValue;
            var eventLoopSchedulerExecuted = new TaskCompletionSource<bool>();
            var eventLoopScheduler = new EventLoopScheduler(threadStart =>
                {
                    var thread = new Thread(threadStart);
                    actualThreadId = thread.ManagedThreadId;
                    return thread;
                });


            eventLoopScheduler.Schedule(() => { expectedThreadId = Thread.CurrentThread.ManagedThreadId; eventLoopSchedulerExecuted.SetResult(true); });
            await eventLoopSchedulerExecuted.Task;

            Assert.Equal(actualThreadId, expectedThreadId);
        }
    }
}
