﻿using System;
using System.Text;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common;
using NUnit.Framework;

namespace Eventstore.ClientAPI.Tests
{
    [TestFixture, Category("LongRunning")]
    public class happy_case_catching_up_to_link_to_events_auto_ack : SpecificationWithConnection
    {
        private readonly string StreamName = Guid.NewGuid().ToString();
        private readonly string GroupName = Guid.NewGuid().ToString();
        private const int BufferCount = 10;
        private const int EventWriteCount = BufferCount * 2;

        private readonly ManualResetEvent _eventsReceived = new ManualResetEvent(false);
        private int _eventReceivedCount;

        protected override void When()
        {
        }

        [Test]
        public void Test()
        {
            var settings = PersistentSubscriptionSettings
                .Create()
                .StartFromBeginning()
                .ResolveLinkTos()
                .Build();
            for (var i = 0; i < EventWriteCount; i++)
            {
                var eventData = new EventData(Guid.NewGuid(), "SomeEvent", false, new byte[0], new byte[0]);
                _conn.AppendToStreamAsync(StreamName + "original", ExpectedVersion.Any, DefaultData.AdminCredentials, eventData).Wait();
            }

            _conn.CreatePersistentSubscriptionAsync(StreamName, GroupName, settings, DefaultData.AdminCredentials)
                .Wait();
            _conn.ConnectToPersistentSubscription(StreamName, GroupName,
                (subscription, resolvedEvent) =>
                {
                    if (Interlocked.Increment(ref _eventReceivedCount) == EventWriteCount)
                    {
                        _eventsReceived.Set();
                    }
                },
                (sub, reason, exception) =>
                        Console.WriteLine("Subscription dropped (reason:{0}, exception:{1}).", reason, exception),
                userCredentials: DefaultData.AdminCredentials,
                autoAck: true);
            for (var i = 0; i < EventWriteCount; i++)
            {

                var eventData = new EventData(Guid.NewGuid(), SystemEventTypes.LinkTo, false,
                    Encoding.UTF8.GetBytes(i + "@" + StreamName + "original"), null);
                _conn.AppendToStreamAsync(StreamName, ExpectedVersion.Any, DefaultData.AdminCredentials, eventData).Wait();
            }

            if (!_eventsReceived.WaitOne(TimeSpan.FromSeconds(5)))
            {
                throw new Exception("Timed out waiting for events.");
            }
        }
    }
}