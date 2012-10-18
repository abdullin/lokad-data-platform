using System;
using NUnit.Framework;

namespace Platform.Core.Tests
{
    public class when_subscribing
    {
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void null_handler_throws_exception()
        {
            var bus = new InMemoryBus("Test");
            FakeHandleService fake = null;
            bus.Subscribe<FakeBeginMessage>(fake);
        }
    }

    public class when_publishing
    {
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void null_message_throws_exception()
        {
            var bus = new InMemoryBus("Test");
            bus.Publish(null);
        }

        [Test]
        public void subscribed_and_then_unsubscribed_handler_does_not_handle_message()
        {
            // GIVEN
            var bus = new InMemoryBus("Test");
            var handler = new SingleHandler();

            bus.Subscribe(handler);
            bus.Unsubscribe(handler);

            // WHEN
            bus.Publish(new FakeBeginMessage());
            // EXPECT
            Assert.IsFalse(handler.HandledMessage);
        }

        [Test]
        public void subscribed_handler_handles_message()
        {
            // GIVEN
            var bus = new InMemoryBus("Test");
            var handler = new SingleHandler();

            bus.Subscribe(handler);

            // WHEN
            bus.Publish(new FakeBeginMessage());
            // EXPECT
            Assert.IsTrue(handler.HandledMessage);
        }

        [Test]
        public void subscribed_handler_not_handles_message()
        {
            // GIVEN
            var bus = new InMemoryBus("Test");
            var handler = new SingleHandler();
            bus.Subscribe(handler);

            //WHEN

            //EXPECT
            Assert.IsFalse(handler.HandledMessage);
        }

        [Test]
        public void subscribed_handler_more_handles_message()
        {
            // GIVEN
            var bus = new InMemoryBus("Test");
            var handler = new SingleHandler();
            bus.Subscribe(handler);

            //WHEN
            const int publichCount = 5;
            for (int i = 0; i < publichCount; i++)
            {
                bus.Publish(new FakeBeginMessage());
            }

            //EXPECT
            Assert.AreEqual(publichCount, handler.HandledCountMessage);
        }
    }

    public class when_unsubscribeing
    {
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void null_handler_throws_exception()
        {
            var bus = new InMemoryBus("Test");
            bus.Unsubscribe((SingleHandler)null);
        }
    }

    public class FakeBeginMessage : Message { }
    public class FakeEndMessage : Message { }

    public class SingleHandler : IHandle<FakeBeginMessage>
    {
        public void Handle(FakeBeginMessage message)
        {
            HandledMessage = true;
            HandledCountMessage++;
        }

        public bool HandledMessage { get; private set; }
        public int HandledCountMessage { get; private set; }
    }

    public class FakeHandleService : IHandle<FakeBeginMessage>, IHandle<FakeEndMessage>
    {
        public bool? State = null;
        public void Handle(FakeBeginMessage message)
        {
            HandledMessage = true;
            State = true;
        }

        public void Handle(FakeEndMessage message)
        {
            HandledMessage = true;
            State = false;
        }

        public bool HandledMessage { get; private set; }
    }
}