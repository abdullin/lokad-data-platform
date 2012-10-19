using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Platform.Core.Tests
{
    public class when_start
    {
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void multiple_start()
        {
            //GIVEN
            var mainQueue = new QueuedHandler(null, "Main Queue");
            mainQueue.Start();

            //WHEN
            mainQueue.Start();
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void starting_stoped_server()
        {
            //GIVEN
            var mainQueue = new QueuedHandler(null, "Main Queue");
            mainQueue.Start();
            Thread.Sleep(1000);
            mainQueue.Stop();

            //WHEN
            mainQueue.Start();
        }
    }

    public class when_enqueue
    {
        [Test]
        public void messages_handler()
        {
            //GIVEN
            var message = new QueuedTestMessage1();
            var controller = new QueuedTestController1();
            var mainQueue = new QueuedHandler(controller, "Main Queue");
            mainQueue.Start();

            //WHEN
            mainQueue.Enqueue(message);

            //EXPECT
            Assert.AreEqual(true, controller.MessageHandled(10000));
            CollectionAssert.AreEqual(new[] { message }, controller.HandledMessages);
        }

        [Test]
        public void messages_handler_when_not_starting()
        {
            //GIVEN
            var controller = new QueuedTestController1();
            var mainQueue = new QueuedHandler(controller, "Main Queue");

            //WHEN
            mainQueue.Enqueue(new QueuedTestMessage1());

            //EXPECT
            Assert.AreEqual(false, controller.MessageHandled());
            CollectionAssert.IsEmpty(controller.HandledMessages);
        }

        [Test]
        public void messages_handler_when_stoped()
        {
            //GIVEN
            var controller = new QueuedTestController1();
            var mainQueue = new QueuedHandler(controller, "Main Queue");
            mainQueue.Start();
            mainQueue.Stop();

            //WHEN
            mainQueue.Enqueue(new QueuedTestMessage1());

            //EXPECT
            Assert.AreEqual(false, controller.MessageHandled());
            CollectionAssert.IsEmpty(controller.HandledMessages);
        }

        [Test]
        public void multiple_messages_handler()
        {
            //GIVEN
            var controller = new QueuedTestController1();
            var mainQueue = new QueuedHandler(controller, "Main Queue");
            mainQueue.Start();

            //WHEN
            var msg1 = new QueuedTestMessage1();
            var msg2 = new QueuedTestMessage2();
            mainQueue.Enqueue(msg1);
            mainQueue.Enqueue(msg2);

            //EXPECT
            Assert.AreEqual(true, controller.MessageHandled());
            CollectionAssert.AreEqual(new List<Message> { msg1, msg2 }, controller.HandledMessages);
        }

    }

    public class when_stop
    {
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void throw_exception_on_timeout()
        {
            //GIVEN
            var controller = new SleepTestController1();
            var mainQueue = new QueuedHandler(controller, "Main Queue", 1000);
            mainQueue.Start();
            mainQueue.Enqueue(new QueuedTestMessage1());

            //WHEN
            Assert.AreEqual(false, controller.MessageHandled());
            mainQueue.Stop();
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void multiple_stoped()
        {
            //GIVEN
            var controller = new SleepTestController1();
            var mainQueue = new QueuedHandler(controller, "Main Queue", 1000);
            mainQueue.Start();
            mainQueue.Enqueue(new QueuedTestMessage1());
            controller.MessageHandled();
            try
            {
                mainQueue.Stop();
            }
            catch (InvalidOperationException) { }

            //WHEN
            mainQueue.Stop();
        }

        [Test]
        public void stoped_not_starting_server()
        {
            //GIVEN
            var controller = new SleepTestController1();
            var mainQueue = new QueuedHandler(controller, "Main Queue", 1000);

            //WHEN
            mainQueue.Stop();
        }
    }

    public class SleepTestController1 : IHandle<Message>
    {
        ManualResetEvent handledState;
        public SleepTestController1()
        {
            handledState = new ManualResetEvent(false);
        }

        public bool MessageHandled(int timeOut = 1000)
        {
            return handledState.WaitOne(timeOut);
        }

        public void Handle(Message message)
        {
            Thread.Sleep(int.MaxValue);
            handledState.Set();
        }
    }

    public class QueuedTestMessage1 : Message { }
    public class QueuedTestMessage2 : Message { }

    public class QueuedTestController1 : IHandle<Message>
    {
        public IList<Message> HandledMessages = new List<Message>();
        ManualResetEvent handledState;
        public QueuedTestController1()
        {
            handledState = new ManualResetEvent(false);
        }

        public void Handle(Message message)
        {
            HandledMessages.Add(message);
            handledState.Set();
        }

        public bool MessageHandled(int timeOut = 1000)
        {
            return handledState.WaitOne(timeOut);
        }
    }
}