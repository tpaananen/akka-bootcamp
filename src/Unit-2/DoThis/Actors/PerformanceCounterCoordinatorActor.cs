using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterCoordinatorActor : ReceiveActor
    {

        private static readonly IReadOnlyDictionary<CounterType, Func<PerformanceCounter>> PerformanceCounterGenerators =
            new Dictionary<CounterType, Func<PerformanceCounter>>
            {
                { CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true)},
                { CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true)},
                { CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)},
            };

        /// <summary>
        /// Methods for creating new <see cref="Series"/> with distinct colors and names
        /// corresponding to each <see cref="PerformanceCounter"/>
        /// </summary>
        private static readonly IReadOnlyDictionary<CounterType, Func<Series>> CounterSeries =
            new Dictionary<CounterType, Func<Series>>()
            {
                {
                    CounterType.Cpu, () => new Series(CounterType.Cpu.ToString())
                    {
                        ChartType = SeriesChartType.SplineArea,
                        Color = Color.DarkGreen
                    }
                },
                {
                    CounterType.Memory, () =>
                        new Series(CounterType.Memory.ToString())
                        {
                            ChartType = SeriesChartType.FastLine,
                            Color = Color.MediumBlue
                        }
                },
                {
                    CounterType.Disk, () =>
                        new Series(CounterType.Disk.ToString())
                        {
                            ChartType = SeriesChartType.SplineArea,
                            Color = Color.DarkRed
                        }
                },
            };

        private readonly Dictionary<CounterType, IActorRef> _counterActors;

        private readonly IActorRef _chartingActor;

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor) :
            this(chartingActor, new Dictionary<CounterType, IActorRef>())
        {
        }

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor, Dictionary<CounterType, IActorRef> counterActors)
        {
            _chartingActor = chartingActor;
            _counterActors = counterActors;

            Receive<Watch>(watch => ManageSubscribtion(watch));
            Receive<Unwatch>(unwatch => ManageUnsubscription(unwatch));
        }

        private void ManageUnsubscription(Unwatch unwatch)
        {
            IActorRef actorRef;
            if (!_counterActors.TryGetValue(unwatch.Counter, out actorRef))
            {
                return;
            }

            // unsubscribe the ChartingActor from receiving anymore updates
            actorRef.Tell(new UnsubscribeCounter(unwatch.Counter, _chartingActor));

            // remove this series from the ChartingActor
            _chartingActor.Tell(new ChartingActor.RemoveSeries(unwatch.Counter.ToString()));
        }

        private void ManageSubscribtion(Watch watch)
        {
            IActorRef actorRef;
            if (!_counterActors.TryGetValue(watch.Counter, out actorRef))
            {
                // create a child actor to monitor this counter if one doesn't exist already
                var func = PerformanceCounterGenerators[watch.Counter];
                actorRef = Context.ActorOf(Props.Create(() => new PerformanceCounterActor(watch.Counter.ToString(), func)));
                _counterActors[watch.Counter] = actorRef;
            }

            // register this series with the ChartingActor
            _chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[watch.Counter]()));

            // tell the counter actor to begin publishing its statistics to the _chartingActor
            actorRef.Tell(new SubscribeCounter(watch.Counter, _chartingActor));
        }

        #region Message types

        /// <summary>
        /// Subscribe the <see cref="ChartingActor"/> to updates for <see cref="Counter"/>.
        /// </summary>
        public class Watch
        {
            public Watch(CounterType counter)
            {
                Counter = counter;
            }

            public CounterType Counter { get; private set; }
        }

        /// <summary>
        /// Unsubscribe the <see cref="ChartingActor"/> to updates for <see cref="Counter"/>.
        /// </summary>
        public class Unwatch
        {
            public Unwatch(CounterType counter)
            {
                Counter = counter;
            }

            public CounterType Counter { get; private set; }
        }

        #endregion

    }
}
