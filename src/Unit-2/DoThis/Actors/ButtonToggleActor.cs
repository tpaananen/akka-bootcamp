using System.Windows.Forms;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ButtonToggleActor : ReceiveActor
    {
        private Button _button;
        private readonly CounterType _counterType;
        private bool _state;
        private readonly IActorRef _coordinatorActor;

        public ButtonToggleActor(IActorRef coordinatorActor, Button button, CounterType counterType, bool defaultState)
        {
            _coordinatorActor = coordinatorActor;
            _button = button;
            _counterType = counterType;
            _state = defaultState;

            if (defaultState)
            {
                Subscribe();
            }

            Receive<Toggle>(x =>
            {
                if (_state)
                {
                    Unsubscribe();
                }
                else
                {
                    Subscribe();
                }
               
                FlipToggle();
            });
        }

        private void Subscribe()
        {
            _coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Watch(_counterType));
        }

        private void Unsubscribe()
        {
            _coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Unwatch(_counterType));
        }

        private void FlipToggle()
        {
            //flip the toggle
            _state = !_state;

            //change the text of the button
            _button.Text = string.Format("{0} ({1})", _counterType.ToString().ToUpperInvariant(),
                                                      _state ? "ON" : "OFF");
        }

        public class Toggle
        {
        }
    }
}
