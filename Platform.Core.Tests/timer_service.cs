using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Platform.Node.Services.Timer;
// ReSharper disable InconsistentNaming

namespace Platform.Core.Tests
{
    public class timer_service2 : IHandle<timer_service2.TestReply>
    {
        public class FakeScheduler : IScheduler
        {
            public DateTime UtcNow { get; set; }

 
            List<Tuple<DateTime,Action>> _schedule= new List<Tuple<DateTime, Action>>(); 
            public void Dispose()
            {
                
            }

            public void Schedule(TimeSpan fireIn, Action<IScheduler, object> action, object state)
            {
                _schedule.Add(Tuple.Create<DateTime, Action>(UtcNow + fireIn, ()=>action(this, state)));
            }

            public void Process(DateTime utc)
            {
                var toTrigger = _schedule.Where(t => t.Item1 <= utc).ToArray();
                foreach (var tuple in toTrigger)
                {
                    tuple.Item2();
                    _schedule.Remove(tuple);
                }

            }
        }

        private TimerService _service;

        [SetUp]
        public void Setup()
        {

            _scheduler = new FakeScheduler() { UtcNow = new DateTime(2000, 1, 1) };
            _deliveredMessages = new List<string>();
            _service = new TimerService(_scheduler);

        }

        private FakeScheduler _scheduler;

        private IList<string> _deliveredMessages;

        public class TestReply : Message
        {
            public string Message { get; set; } 
        }

        public class ReplyBackEnvelope : IEnvelope
        {
            private object sender;

            public ReplyBackEnvelope(object sender)
            {
                this.sender = sender;
            }

            public void ReplyWith<T>(T message) where T : Message
            {
                ((IHandle<T>)sender).Handle(message);
            }
        }


        [Test]
        public void multiple_simultaneous_messages_should_trigger_at_same_time()
        {
            Schedule("msg1", 10);
            Schedule("msg2", 10);
            Schedule("msg3", 10);

            SetTimeAndProcess(9);
            CollectionAssert.IsEmpty(_deliveredMessages);
            SetTimeAndProcess(11);
            CollectionAssert.AreEquivalent(new[] {"msg1", "msg2", "msg3"}, _deliveredMessages);

        }

        [Test]
        public void multiple_simultaneous_messages_should_trigger_at_different_time()
        {
            Schedule("msg1", 10);
            Schedule("msg2", 12);
            Schedule("msg3", 14);

            SetTimeAndProcess(9);
            CollectionAssert.IsEmpty(_deliveredMessages);
            SetTimeAndProcess(11);
            CollectionAssert.AreEquivalent(new[] { "msg1" }, _deliveredMessages);
            SetTimeAndProcess(13);
            CollectionAssert.AreEquivalent(new[] { "msg1", "msg2" }, _deliveredMessages);
            SetTimeAndProcess(15);
            CollectionAssert.AreEquivalent(new[] { "msg1", "msg2", "msg3" }, _deliveredMessages);
        }

        [Test]
        public void timer_should_respond_with_correct_messages()
        {
            Schedule("msg1", 10);

            SetTimeAndProcess(8);
            CollectionAssert.IsEmpty(_deliveredMessages);
            SetTimeAndProcess(9);
            CollectionAssert.IsEmpty(_deliveredMessages);
            SetTimeAndProcess(10);
            CollectionAssert.AreEquivalent(new[] { "msg1" }, _deliveredMessages);
        }


        private void Schedule(string msg1, int p1)
        {
            _service.Handle(TimerMessage.Schedule.Create(TimeSpan.FromMilliseconds(p1),new ReplyBackEnvelope(this), new TestReply(){ Message = msg1} ));
        }

        private void SetTimeAndProcess(int ms)
        {
            _scheduler.Process(new DateTime(2000, 1, 1).AddMilliseconds(ms));

        }

        public void Handle(TestReply message)
        {
            _deliveredMessages.Add(message.Message);
        }
    }
}