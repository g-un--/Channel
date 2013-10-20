using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Channel.Tests
{
    public class LearnSubjectTests
    {
        [Fact]
        public async Task SubjectShouldForwardCallsToObservablesOnSameThread()
        {
            int actualThreadId = int.MaxValue;
            int expectedThreadId = int.MinValue;
            var subject = new Subject<object>();
            subject.Subscribe(obj => { expectedThreadId = Thread.CurrentThread.ManagedThreadId; });

            await Task.Factory.StartNew(() =>
            {
                actualThreadId = Thread.CurrentThread.ManagedThreadId;
                subject.OnNext(new { });
            });

            Assert.Equal(actualThreadId, expectedThreadId);
        }

        [Fact]
        public void ObservableZipShouldForwardCallsOnTheLastCallerThread()
        {
            int expectedThreadId = int.MinValue;
            int rightSubjectThreadId = int.MaxValue;
            int leftSubjectThreadId = 0;
            var leftSubject = new Subject<object>();
            var rightSubject = new Subject<object>();
            leftSubject.Zip(rightSubject, (left, right) => new { }).Subscribe(rez => expectedThreadId = Thread.CurrentThread.ManagedThreadId);

            ThreadStart leftSubjectOnNext = new ThreadStart(() =>
                {
                    leftSubjectThreadId = Thread.CurrentThread.ManagedThreadId;
                    leftSubject.OnNext(new { });
                });

            ThreadStart rightSubjectOnNext = new ThreadStart(() =>
            {
                rightSubjectThreadId = Thread.CurrentThread.ManagedThreadId;
                rightSubject.OnNext(new { });
            });

            var leftSubjectThread = new Thread(leftSubjectOnNext);
            var rightSubjectThread = new Thread(rightSubjectOnNext);
         
            leftSubjectThread.Start();
            rightSubjectThread.Start();

            leftSubjectThread.Join();
            rightSubjectThread.Join();
            
            Assert.NotEqual(leftSubjectThreadId, rightSubjectThreadId);
            Assert.Equal(rightSubjectThreadId, expectedThreadId);
        }
    }
}
