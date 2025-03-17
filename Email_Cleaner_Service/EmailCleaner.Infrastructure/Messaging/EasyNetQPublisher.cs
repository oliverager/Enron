using EasyNetQ;

namespace EmailCleaner.Infrastructure.Messaging
{
    public class EasyNetQPublisher
    {
        private readonly IBus _bus;
        
        public EasyNetQPublisher(IBus bus)
        {
            _bus = bus;
        }

        public void Publish<T>(T message)
        {
            _bus.PubSub.Publish(message);
        }
    }
}