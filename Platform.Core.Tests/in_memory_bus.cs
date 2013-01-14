using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Platform.Node;

namespace Platform.Core.Tests
{
    public abstract class tests_with_bus
    {
        protected InMemoryBus Bus;

        [SetUp]
        public virtual void Setup()
        {
            Bus = new InMemoryBus("tests");
        }
        [TearDown]
        public virtual void Teardown()
        {
            Bus = null;
        }

    }

    public class when_subscribing : tests_with_bus
    {
        [Test]
        public void null_handler_throws_exception()
        {
            Assert.Throws<ArgumentNullException>(() => Bus.Subscribe<TestMessage1>(null));
        }
    }

    public class when_publishing : tests_with_bus
    {
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void null_message_throws_exception()
        {
            Bus.Publish(null);
        }

        [Test]
        public void subscribed_and_then_unsubscribed_handler_does_not_handle_message()
        {
            // GIVEN
            var handler = new TestHandler1();

            Bus.Subscribe(handler);
            Bus.Unsubscribe(handler);

            // WHEN
            Bus.Publish(new TestMessage1());
            // EXPECT
            Assert.IsTrue(handler.DidntHaveAnyMessages);
        }

        [Test]
        public void subscribed_handler_handles_message()
        {
            // GIVEN
            var handler = new TestHandler1();
            Bus.Subscribe(handler);

            // WHEN
            var message = new TestMessage1();
            Bus.Publish(message);
            // EXPECT
            CollectionAssert.AreEqual(new[] { message }, handler.HandledMessages);
        }

        [Test]
        public void subscribed_handler_handles_multiple_messages()
        {
            // GIVEN


            var handler = new TestHandler1();
            Bus.Subscribe(handler);

            var messages = Enumerable.Repeat(new TestMessage1(), 5).ToArray();
            //WHEN
            foreach (var m in messages)
            {
                Bus.Publish(m);
            }

            CollectionAssert.AreEqual(messages, handler.HandledMessages);

        }
        [Test]
        public void subscribed_handler_does_not_handle_other_messages()
        {
            // GIVEN
            var handler = new TestHandler1();
            Bus.Subscribe(handler);
            // WHEN
            Bus.Publish(new TestMessage2());
            // EXPECT
            Assert.IsTrue(handler.DidntHaveAnyMessages);
        }

        [Test]
        public void subscribed_handler_handle_child_messages()
        {
            // GIVEN
            var handler = new ParentHandler();
            Bus.Subscribe(handler);
            // WHEN
            var child = new ChildMessage();
            Bus.Publish(child);
            // EXPECT
            CollectionAssert.AreEqual(new[] { child }, handler.HandledMessages);
        }

        [Test]
        public void subscribed_handler_parent_handle_does_not_messages()
        {
            // GIVEN
            var handler = new ChildHandler();
            Bus.Subscribe(handler);
            // WHEN
            Bus.Publish(new ParentMessage());
            // EXPECT
            CollectionAssert.IsEmpty(handler.HandledMessages);
        }

        [Test]
        public void if_subscribed_handler_repeatedly_get_one_message()
        {
            // GIVEN
            var handler = new TestHandler1();
            Bus.Subscribe(handler);
            Bus.Subscribe(handler);
            // WHEN
            var m = new TestMessage1();
            Bus.Publish(m);
            // EXPECT
            CollectionAssert.AreEqual(new[] { m }, handler.HandledMessages);
        }


        [Test]
        public void subscribed_parent_and_child_handler_handle_parent_messages()
        {
            // GIVEN
            var parentHandler = new ParentHandler();
            var childHandler = new ChildHandler();
            Bus.Subscribe(parentHandler);
            Bus.Subscribe(childHandler);
            // WHEN
            var parentMessage = new ParentMessage();
            Bus.Publish(parentMessage);
            // EXPECT
            CollectionAssert.IsEmpty(childHandler.HandledMessages);
            CollectionAssert.AreEqual(new[]{parentMessage},parentHandler.HandledMessages);
        }

        [Test]
        public void subscribed_parent_and_child_handler_handle_child_messages()
        {
            // GIVEN
            var parentHandler = new ParentHandler();
            var childHandler = new ChildHandler();
            Bus.Subscribe(parentHandler);
            Bus.Subscribe(childHandler);
            // WHEN
            var childMessage = new ChildMessage();
            Bus.Publish(childMessage);
            // EXPECT
            CollectionAssert.AreEqual(new[] { childMessage }, parentHandler.HandledMessages);
            CollectionAssert.AreEqual(childHandler.HandledMessages, parentHandler.HandledMessages);
        }
    }

    public class when_unsubscribeing
    {
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void null_handler_throws_exception()
        {
            var bus = new InMemoryBus("Test");
            bus.Unsubscribe((TestHandler1)null);
        }
    }

    public abstract class TestHandlerBase
    {
        public IList<Message> HandledMessages = new List<Message>();

        public void Handled(Message message)
        {
            HandledMessages.Add(message);
        }
        public bool DidntHaveAnyMessages { get { return !HandledMessages.Any(); } }
    }


    public class TestMessage1 : Message { }

    public class TestHandler1 : TestHandlerBase, IHandle<TestMessage1>
    {
        public void Handle(TestMessage1 message)
        {
            Handled(message);
        }
    }

    public class TestMessage2 : Message { }

    public class TestHandler2 : TestHandlerBase, IHandle<TestMessage2>
    {
        public void Handle(TestMessage2 message)
        {
            Handled(message);
        }
    }

    public class ParentMessage : Message { }

    public class ParentHandler : TestHandlerBase, IHandle<ParentMessage>
    {
        public void Handle(ParentMessage message)
        {
            Handled(message);
        }
    }

    public class ChildMessage : ParentMessage { }

    public class ChildHandler : TestHandlerBase, IHandle<ChildMessage>
    {
        public void Handle(ChildMessage message)
        {
            Handled(message);
        }
    }





}