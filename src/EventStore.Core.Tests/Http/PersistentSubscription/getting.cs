using System;
using System.Net;
using EventStore.Transport.Http;
using EventStore.Core.Tests.Helpers;
using EventStore.Core.Tests.Http.Users.users;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using HttpStatusCode = System.Net.HttpStatusCode;

// ReSharper disable InconsistentNaming

namespace EventStore.Core.Tests.Http.PersistentSubscription
{

    [TestFixture, Category("LongRunning")]
    class when_getting_messages_from_an_empty_subscription_return_0_messages : with_admin_user
    {
        private HttpWebResponse _response;

        protected override void Given()
        {
            _response = MakeJsonPut(
               "/subscriptions/stream/groupname334",
               new
               {
                   ResolveLinkTos = true
               },
               _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
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

    [TestFixture, Category("LongRunning")]
    class when_getting_messages_from_a_subscription_with_n_messags_return_n_messages : with_admin_user
    {
        private HttpWebResponse _response;

        protected override void Given()
        {
            _response = MakeArrayEventsPost(
                         TestStream,
                         new object[]
                            {
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { A = "1" } },
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { B = "2" } },
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { C = "3" } },
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { D = "4" } }
                            },
                         _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
            _response = MakeJsonPut(
                "/subscriptions/" + TestStream.Substring(8) + "/groupname337",
                new
                {
                    ResolveLinkTos = true
                },
                _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);


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

    [TestFixture, Category("LongRunning")]
    class when_getting_messages_from_a_subscription_with_more_than_n_messags_return_n_messages : with_admin_user
    {
        private HttpWebResponse _response;

        protected override void Given()
        {
            _response = MakeArrayEventsPost(
                            TestStream,
                            new object[]
                            {
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { A = "1" } },
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { B = "2" } },
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { C = "3" } },
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { D = "4" } }
                            },
                            _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
            _response = MakeJsonPut(
              "/subscriptions/" + TestStream.Substring(8) + "/groupname451",
              new
              {
                  ResolveLinkTos = true
              },
              _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);

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
    [TestFixture, Category("LongRunning")]
    class when_getting_messages_from_a_subscription_with_less_than_n_messags_return_all_messages : with_admin_user
    {
        private HttpWebResponse _response;

        protected override void Given()
        {
            _response = MakeArrayEventsPost(
                            TestStream,
                            new object[]
                            {
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { A = "1" } },
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { B = "2" } },
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { C = "3" } },
                                new { EventId = Guid.NewGuid(), EventType = "event-type", Data = new { D = "4" } }
                            },
                            _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
            _response = MakeJsonPut(
              "/subscriptions/" + TestStream.Substring(8) + "/groupname452",
              new
              {
                  ResolveLinkTos = true
              },
              _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
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
    class when_getting_messages_from_a_subscription_with_unspecified_count : with_admin_user
    {
        private HttpWebResponse _response;

        protected override void Given()
        {
            _response = MakeJsonPut(
              "/subscriptions/stream/groupname453",
              new
              {
                  ResolveLinkTos = true
              },
              _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
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
    [TestFixture, Category("LongRunning")]
    class when_getting_messages_from_a_subscription_with_a_negative_count : with_admin_user
    {
        private HttpWebResponse _response;

        protected override void Given()
        {
            _response = MakeJsonPut(
              "/subscriptions/stream/groupname454",
              new
              {
                  ResolveLinkTos = true
              },
              _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
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
    [TestFixture, Category("LongRunning")]
    class when_getting_messages_from_a_subscription_with_a_count_of_0 : with_admin_user
    {
        private HttpWebResponse _response;

        protected override void Given()
        {
            _response = MakeJsonPut(
              "/subscriptions/stream/groupname455",
              new
              {
                  ResolveLinkTos = true
              },
              _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
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
    [TestFixture, Category("LongRunning")]
    class when_getting_messages_from_a_subscription_with_count_more_than_100 : with_admin_user
    {
        private HttpWebResponse _response;

        protected override void Given()
        {
            _response = MakeJsonPut(
              "/subscriptions/stream/groupname456",
              new
              {
                  ResolveLinkTos = true
              },
              _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
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
    [TestFixture, Category("LongRunning")]
    class when_getting_messages_from_a_subscription_with_count_not_an_integer : with_admin_user
    {
        private HttpWebResponse _response;

        protected override void Given()
        {
            _response = MakeJsonPut(
              "/subscriptions/stream/groupname457",
              new
              {
                  ResolveLinkTos = true
              },
              _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
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
    [TestFixture, Category("LongRunning")]
    class when_getting_messages_from_a_subscription_with_count_not_a_number : with_admin_user
    {
        private HttpWebResponse _response;

        protected override void Given()
        {
            _response = MakeJsonPut(
              "/subscriptions/stream/groupname458",
              new
              {
                  ResolveLinkTos = true
              },
              _admin);
            Assert.AreEqual(HttpStatusCode.Created, _response.StatusCode);
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