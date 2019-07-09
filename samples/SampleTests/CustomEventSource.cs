using System.Diagnostics.Tracing;

namespace SampleTests
{
    [EventSource(Name = "Custom-EventSource")]
    public class CustomEventSource : EventSource
    {
        public static readonly CustomEventSource Log = new CustomEventSource();
        private CustomEventSource()
        {
        }

        [Event(eventId: 1)]
        public void CustomEvent(string payload)
        {
            WriteEvent(1, payload);
        }
    }
}
