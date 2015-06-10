using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterActor : ReceiveActor
    {
        private readonly string _seriesName;
        private readonly Func<PerformanceCounter> _perfCounterGenerator;
        private readonly ICancelable _cancelable;
        private readonly HashSet<IActorRef> _subscribtions;

        private PerformanceCounter _performanceCounter;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> perfCounterGenerator)
        {
            _seriesName = seriesName;
            _perfCounterGenerator = perfCounterGenerator;
            _performanceCounter = perfCounterGenerator();
            _cancelable = new Cancelable(Context.System.Scheduler);
            _subscribtions = new HashSet<IActorRef>();

            InitializeReceive();
        }

        protected override void PreStart()
        {
            //create a new instance of the performance counter
            _performanceCounter = _perfCounterGenerator();
            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromMilliseconds(250), 
                TimeSpan.FromMilliseconds(250), 
                Self,
                new GatherMetrics(), 
                Self, 
                _cancelable);
        }

        protected override void PostStop()
        {
            try
            {
                //terminate the scheduled task
                _cancelable.Cancel(false);
                _performanceCounter.Dispose();
            }
            catch
            {
                //don't care about additional "ObjectDisposed" exceptions
            }
            finally
            {
                base.PostStop();
            }
        }

        private void InitializeReceive()
        {
            Receive<GatherMetrics>(x => GatherMetrics());
            Receive<SubscribeCounter>(x => TakeSubscription(x));
            Receive<UnsubscribeCounter>(x => RemoveSubscription(x));
        }

        private void GatherMetrics()
        {
            if (_subscribtions.Count == 0)
            {
                return;
            }

            var counter = _performanceCounter.NextValue();
            var metric = new Metric(_seriesName, counter);
            foreach (var sub in _subscribtions)
            {
                sub.Tell(metric);
            }
        }

        private void TakeSubscription(SubscribeCounter message)
        {
            _subscribtions.Add(message.Subscriber);
        }

        private void RemoveSubscription(UnsubscribeCounter message)
        {
            _subscribtions.Remove(message.Subscriber);
        }
    }
}
