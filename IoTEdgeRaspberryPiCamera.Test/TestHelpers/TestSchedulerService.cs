using System.Reactive.Concurrency;
using IoTEdgeRaspberryPiCamera.Services.Scheduler;
using Microsoft.Reactive.Testing;

namespace IoTEdgeRaspberryPiCamera.Test.TestHelpers
{
    public class TestSchedulerProvider : TestScheduler, ISchedulerProvider
    {
        public IScheduler NewThread { get; }
        public IScheduler TaskPool { get; }
        public IScheduler ThreadPool { get; }

        public TestSchedulerProvider()
        {
            NewThread = this;
            TaskPool = this;
            ThreadPool = this;
        }
    }
}