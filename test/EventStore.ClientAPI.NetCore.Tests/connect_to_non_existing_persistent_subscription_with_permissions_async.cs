﻿using System;
using NUnit.Framework;

namespace Eventstore.ClientAPI.Tests
{
    [TestFixture, Category("LongRunning")]
    public class connect_to_non_existing_persistent_subscription_with_permissions_async : SpecificationWithConnection
    {
        private Exception _innerEx;

        protected override void When()
        {
            _innerEx = Assert.Throws<AggregateException>(() =>
            {
                _conn.ConnectToPersistentSubscriptionAsync(
                     "nonexisting2",
                     "foo",
                     (sub, e) => Console.Write("appeared"),
                     (sub, reason, ex) =>
                     {
                     }).Wait();
            }).InnerException;
        }

        [Test]
        public void the_subscription_fails_to_connect_with_argument_exception()
        {
            Assert.IsInstanceOf<AggregateException>(_innerEx);
            Assert.IsInstanceOf<ArgumentException>(_innerEx.InnerException);
        }
    }
}