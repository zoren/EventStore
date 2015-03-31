using System;
using System.Net;
using System.Text.RegularExpressions;
using EventStore.Core.Tests.Http.Users.users;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using HttpStatusCode = System.Net.HttpStatusCode;
using EventStore.Transport.Http;

// ReSharper disable InconsistentNaming

namespace EventStore.Core.Tests.Http.PersistentSubscription
{
    [TestFixture, Category("LongRunning")]
    class with_subscription_having_events : with_admin_user
    {
        protected List<object> Events;
        protected string SubscriptionPath;
        protected string GroupName;
        
        

        protected override void Given()
        {
            Events = new List<object>
            {
                new {EventId = Guid.NewGuid(), EventType = "event-type", Data = new {A = "1"}},
                new {EventId = Guid.NewGuid(), EventType = "event-type", Data = new {B = "2"}},
                new {EventId = Guid.NewGuid(), EventType = "event-type", Data = new {C = "3"}},
                new {EventId = Guid.NewGuid(), EventType = "event-type", Data = new {D = "4"}}
            };

            var response = MakeArrayEventsPost(
                         TestStream,
                         Events,
                         _admin);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            GroupName = Guid.NewGuid().ToString();
            SubscriptionPath = string.Format("/subscriptions/{0}/{1}",TestStream.Substring(8),GroupName);
            response = MakeJsonPut( SubscriptionPath,
                new
                {
                    ResolveLinkTos = true
                },
                _admin);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        }

        protected override void When()
        {

        }
    }

    class when_getting_messages_from_an_empty_subscription_return_0_messages : with_subscription_having_events
    {
        protected override void Given()
        {

        }

        protected override void When()
        {
        }

        [Test]
        public void returns_0_messages()
        {
           
            //todo: implement test
            //not proven to pass or fail yet
             Assert.Inconclusive("Not Implemented");
        }
    }

    class when_getting_messages_from_a_subscription_with_n_messags_return_n_messages : with_subscription_having_events
    {
        private JObject _response;
        protected override void When()
        {
            _response = GetJson<JObject>(
                                SubscriptionPath + "/messages?count=" + 4 ,
                                ContentType.Any, //todo CLC sort out allowed content typees
                                _admin);
        }

        [Test]
        public void returns_n_messages()
        {
            var count = ((JArray)_response["events"]).Count;
            Assert.AreEqual(Events.Count, count,"Expected {0} events, received {1}", Events.Count, count);
        }
    }

    class when_getting_messages_from_a_subscription_with_more_than_n_messags_return_n_messages : with_subscription_having_events
    {

        protected override void Given()
        {
        }

        protected override void When()
        {
        }

        [Test]
        public void returns_n_messages()
        {
            //todo: implement test
            //not proven to pass or fail yet
            Assert.Inconclusive("Not Implemented");
        }
    }

    class when_getting_messages_from_a_subscription_with_less_than_n_messags_return_all_messages : with_subscription_having_events
    {

        protected override void Given()
        {
        }

        protected override void When()
        {

        }

        [Test]
        public void returns_all_messages()
        {
            //todo: implement test
            //not proven to pass or fail yet
            Assert.Inconclusive("Not Implemented");
        }
    }
    [TestFixture, Category("LongRunning")]
    class when_getting_messages_from_a_subscription_with_unspecified_count : with_subscription_having_events
    {
        protected override void Given()
        {
        }

        protected override void When()
        {
        }

        [Test]
        public void returns_one_message()
        {
            //todo: implement test
            //not proven to pass or fail yet
            Assert.Inconclusive("Not Implemented");
        }
    }

    class when_getting_messages_from_a_subscription_with_a_negative_count : with_subscription_having_events
    {

        protected override void Given()
        {
        }

        protected override void When()
        {

        }

        [Test]
        public void returns_bad_request()
        {
            //todo: implement test
            //not proven to pass or fail yet
            Assert.Inconclusive("Not Implemented");
        }
    }

    class when_getting_messages_from_a_subscription_with_a_count_of_0 : with_subscription_having_events
    {
        protected override void Given()
        {
        }

        protected override void When()
        {
        }

        [Test]
        public void returns_bad_request()
        {
            //todo: implement test
            // not proven to pass or fail yet
            Assert.Inconclusive("Not Implemented");
        }
    }

    class when_getting_messages_from_a_subscription_with_count_more_than_100 : with_subscription_having_events
    {

        protected override void Given()
        {
        }

        protected override void When()
        {
        }

        [Test]
        public void returns_bad_request()
        {
            //todo: implement test
            // not proven to pass or fail yet
            Assert.Inconclusive("Not Implemented");
        }
    }

    class when_getting_messages_from_a_subscription_with_count_not_an_integer : with_subscription_having_events
    {
        protected override void Given()
        {
        }

        protected override void When()
        {
        }

        [Test]
        public void returns_bad_request()
        {
            //todo: implement test
            // not proven to pass or fail yet
            Assert.Inconclusive("Not Implemented");
        }
    }

    class when_getting_messages_from_a_subscription_with_count_not_a_number : with_subscription_having_events
    {

        protected override void Given()
        {
        }

        protected override void When()
        {
        }

        [Test]
        public void returns_bad_request()
        {
            //todo: implement test
            // not proven to pass or fail yet
            Assert.Inconclusive("Not Implemented");
        }
    }
}