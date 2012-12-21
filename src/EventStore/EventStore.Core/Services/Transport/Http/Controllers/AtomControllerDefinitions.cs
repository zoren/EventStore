﻿// Copyright (c) 2012, Event Store LLP
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Text;
using EventStore.Common.Log;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Services.Transport.Http.Codecs;
using EventStore.Transport.Http;
using EventStore.Transport.Http.Atom;
using EventStore.Transport.Http.EntityManagement;
using Newtonsoft.Json;

namespace EventStore.Core.Services.Transport.Http.Controllers
{
    public class AtomController : CommunicationController
    {
        private static readonly HtmlFeedCodec _htmlFeedCodec = new HtmlFeedCodec(); // initialization order matters
        private static readonly ICodec[] ServiceDocCodecs = new[]
                                                            {
                                                                Codec.Xml,
                                                                Codec.ApplicationXml,
                                                                Codec.CreateCustom(Codec.Xml, ContentType.AtomServiceDoc),
                                                                Codec.Json,
                                                                Codec.CreateCustom(Codec.Json, ContentType.AtomServiceDocJson)
                                                            };
        private static readonly ICodec[] AtomCodecs = new[]
                                                      {
                                                          Codec.Xml,
                                                          Codec.ApplicationXml,
                                                          Codec.CreateCustom(Codec.Xml, ContentType.Atom),
                                                          Codec.Json,
                                                          Codec.CreateCustom(Codec.Json, ContentType.AtomJson)
                                                      };
        private static readonly ICodec[] AtomFeedCodecs = new[]
                                                      {
                                                          Codec.Xml,
                                                          Codec.ApplicationXml,
                                                          Codec.CreateCustom(Codec.Xml, ContentType.Atom),
                                                          Codec.Json,
                                                          Codec.CreateCustom(Codec.Json, ContentType.AtomJson),
                                                          _htmlFeedCodec // initialization order matters
                                                      };

        private static readonly ICodec DefaultResponseCodec = Codec.Xml;
        private static readonly ICodec DefaultFeedResponseCodec = _htmlFeedCodec; // initialization order matters

        private readonly GenericController _genericController;
        private readonly AllEventsController _allEventsController;
        private readonly IPublisher _networkSendQueue;

        public AtomController(IPublisher publisher, IPublisher networkSendQueue)
            : base(publisher)
        {
            _networkSendQueue = networkSendQueue;
            _genericController = new GenericController(publisher, networkSendQueue);
            _allEventsController = new AllEventsController(publisher, networkSendQueue);
        }

        protected override void SubscribeCore(IHttpService service, HttpMessagePipe pipe)
        {
            service.RegisterControllerAction(new ControllerAction("/streams",
                                                                  HttpMethod.Get,
                                                                  Codec.NoCodecs,
                                                                  ServiceDocCodecs, 
                                                                  DefaultResponseCodec),
                                             OnGetServiceDocument);
            service.RegisterControllerAction(new ControllerAction("/streams",
                                                                  HttpMethod.Post,
                                                                  AtomCodecs,
                                                                  AtomCodecs,
                                                                  DefaultResponseCodec),
                                             OnCreateStream);
            service.RegisterControllerAction(new ControllerAction("/streams/{stream}",
                                                                  HttpMethod.Delete,
                                                                  AtomCodecs,
                                                                  AtomCodecs,
                                                                  DefaultResponseCodec),
                                             OnDeleteStream);
            service.RegisterControllerAction(new ControllerAction("/streams/{stream}?embed={embed}", 
                                                                  HttpMethod.Get,
                                                                  Codec.NoCodecs,
                                                                  AtomFeedCodecs,
                                                                  DefaultFeedResponseCodec),
                                             OnGetFeedLatest);
            service.RegisterControllerAction(new ControllerAction("/streams/{stream}/range/{start}/{count}?embed={embed}",
                                                                  HttpMethod.Get,
                                                                  Codec.NoCodecs,
                                                                  AtomFeedCodecs,
                                                                  DefaultFeedResponseCodec),
                                             OnGetFeedPage);
            service.RegisterControllerAction(new ControllerAction("/streams/{stream}/{id}",
                                                                  HttpMethod.Get,
                                                                  Codec.NoCodecs,
                                                                  AtomCodecs,
                                                                  DefaultResponseCodec), 
                                             OnGetEntry);
            service.RegisterControllerAction(new ControllerAction("/streams/{stream}",
                                                                  HttpMethod.Post,
                                                                  AtomCodecs,
                                                                  AtomCodecs,
                                                                  DefaultResponseCodec),
                                             OnPostEntry);

            service.RegisterControllerAction(new ControllerAction("/streams/$all",
                                                                  HttpMethod.Get,
                                                                  Codec.NoCodecs,
                                                                  AtomCodecs,
                                                                  DefaultResponseCodec),
                                             OnGetAllBefore);
            service.RegisterControllerAction(new ControllerAction("/streams/$all/{count}",
                                                                  HttpMethod.Get,
                                                                  Codec.NoCodecs,
                                                                  AtomCodecs,
                                                                  DefaultResponseCodec),
                                             OnGetAllBefore);
            service.RegisterControllerAction(new ControllerAction("/streams/$all/before/{pos}/{count}",
                                                                  HttpMethod.Get,
                                                                  Codec.NoCodecs,
                                                                  AtomCodecs,
                                                                  DefaultResponseCodec),
                                             OnGetAllBefore);
            service.RegisterControllerAction(new ControllerAction("/streams/$all/after/{pos}/{count}",
                                                                  HttpMethod.Get,
                                                                  Codec.NoCodecs,
                                                                  AtomCodecs,
                                                                  DefaultResponseCodec),
                                             OnGetAllAfter);
        }

        //SERVICE DOCUMENT

        private void OnGetServiceDocument(HttpEntity entity, UriTemplateMatch match)
        {
            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  entity,
                                                  Format.Atom.ListStreamsCompletedServiceDoc,
                                                  Configure.ListStreamsCompletedServiceDoc);
            Publish(new ClientMessage.ListStreams(envelope));
        }

        //FEED

        private void OnCreateStream(HttpEntity entity, UriTemplateMatch match)
        {
            _genericController.CreateStream(entity);
        }

        private void OnDeleteStream(HttpEntity entity, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            if (string.IsNullOrEmpty(stream))
            {
                SendBadRequest(entity, string.Format("Invalid stream name '{0}'", stream));
                return;
            }

            _genericController.DeleteStream(entity, stream);
        }

        private void OnGetFeedLatest(HttpEntity entity, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            var embed = IsOn(match, "embed", false);
            if (string.IsNullOrEmpty(stream))
            {
                SendBadRequest(entity, string.Format("Invalid stream name '{0}'", stream));
                return;
            }

            OnGetFeedCore(entity, stream, -1, AtomSpecs.FeedPageSize, embed);
        }

        private void OnGetFeedPage(HttpEntity entity, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            var start = match.BoundVariables["start"];
            var count = match.BoundVariables["count"];
            var embed = IsOn(match, "embed", false);

            int startIdx;
            int cnt;

            if (string.IsNullOrEmpty(stream))
            {
                SendBadRequest(entity, string.Format("Invalid stream name '{0}'", stream));
                return;
            }
            if (!int.TryParse(start, out startIdx) || startIdx < -1)
            {
                SendBadRequest(entity, string.Format("'{0}' is not valid start index", start));
                return;
            }
            if (!int.TryParse(count, out cnt) || cnt <= 0)
            {
                SendBadRequest(entity, string.Format("'{0}' is not valid count. Should be positive integer", count));
                return;
            }

            OnGetFeedCore(entity, stream, startIdx, cnt, embed);
        }

        private void OnGetFeedCore(HttpEntity entity, string stream, int start, int count, bool embed)
        {
            _genericController.GetFeedPage(entity, stream, start, count, embed);
        }

        private static bool IsOn(UriTemplateMatch match, string option, bool def)
        {
            var rawValue = match.BoundVariables[option];
            if (string.IsNullOrEmpty(rawValue))
                return def;
            var value = rawValue.ToLowerInvariant();
            return "yes" == value || "true" == value || "1" == value;
        }

        //$ALL

        private void OnGetAllBefore(HttpEntity entity, UriTemplateMatch match)
        {
            var p = match.BoundVariables["pos"];
            var c = match.BoundVariables["count"];

            TFPos position;
            int count;

            if (!string.IsNullOrEmpty(p))
            {
                if (!TFPos.TryParse(p, out position))
                    SendBadRequest(entity, string.Format("Invalid position argument : {0}", p));
            }
            else
            {
                position = TFPos.Invalid;
            }

            if (!string.IsNullOrEmpty(c))
            {
                if (!int.TryParse(c, out count))
                    SendBadRequest(entity, string.Format("Invalid count argument : {0}", c));
            }
            else
            {
                count = AtomSpecs.FeedPageSize;
            }

            _allEventsController.GetAllBefore(entity, position, count);
        }

        private void OnGetAllAfter(HttpEntity entity, UriTemplateMatch match)
        {
            var p = match.BoundVariables["pos"];
            var c = match.BoundVariables["count"];

            TFPos position;
            int count;

            if (string.IsNullOrEmpty(p) || !TFPos.TryParse(p, out position))
            {
                SendBadRequest(entity, string.Format("Invalid position argument : {0}", p));
                return;
            }
            if (string.IsNullOrEmpty(c) || !int.TryParse(c, out count))
            {
                SendBadRequest(entity, string.Format("Invalid count argument : {0}", c));
                return;
            }

            _allEventsController.GetAllAfter(entity, position, count);
        }

        //ENTRY MANIPULATION

        private void OnGetEntry(HttpEntity entity, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            var id = match.BoundVariables["id"];
            int version;
            if (string.IsNullOrEmpty(stream) || !int.TryParse(id, out version))
            {
                SendBadRequest(entity, "Stream must bu non-empty string and id must be integer value");
                return;
            }

            _genericController.GetEntry(entity, stream, version);
        }

        private void OnPostEntry(HttpEntity entity, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            if (string.IsNullOrEmpty(stream))
            {
                SendBadRequest(entity, string.Format("Invalid request. Stream must be non-empty string"));
                return;
            }

            _genericController.PostEntry(entity, stream);
        }
    }

    class HtmlFeedCodec : ICodec, IRichAtomCodec
    {
        public string ContentType {
            get { return "text/html"; }
        }

        public bool CanParse(string format)
        {
            throw new NotImplementedException();
        }

        public bool SuitableForReponse(AcceptComponent component)
        {
            return component.MediaType == "*"
                   || (component.MediaType == "text"
                       && (component.MediaSubtype == "*" || component.MediaSubtype == "html"));
        }

        public T From<T>(string text)
        {
            throw new NotImplementedException();
        }

        public string To<T>(T value)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <script src='/web/es/lib/jquery/jquery-1.8.0.min.js'></script>
    <script src='/web/es/lib/jsrender/jsrender.js'></script>
    <script src='/web/es/js/atom/render.js'></script>
    <script src='/web/es/js/es.tmpl.js'></script>
    <script id='r-head'>
        es.tmpl.renderHead();
    </script>
</head>
<body>
<script>
    var data = " + JsonConvert.SerializeObject(value, Formatting.Indented) + @";
    var templateJs = '/web/es/js/atom/" + value.GetType().Name + @".html';
    $(function() {
        renderHtmlBy(data, templateJs);
    }); 
</script>

<div id='content'>
    <div id='data'></div>
    <script id='r-body'>
    es.tmpl.renderBody();
    </script>
</div>


</body>
</html>
";
        }
    }

    interface IRichAtomCodec
    {
    }


    /*
     * */
    public class GenericController : CommunicationController
    {
        private readonly IPublisher _networkSendQueue;
        private static readonly ILogger Log = LogManager.GetLoggerFor<GenericController>();

        public GenericController(IPublisher publisher, IPublisher networkSendQueue)
            : base(publisher)
        {
            _networkSendQueue = networkSendQueue;
        }

        protected override void SubscribeCore(IHttpService service, HttpMessagePipe pipe)
        {
            //no direct subscriptions
        }

        public void CreateStream(HttpEntity entity)
        {
            entity.Manager.ReadTextRequestAsync(CreateStreamBodyRead,
                                            e => Log.ErrorException(e, "Error while reading request (CREATE stream)"));
        }

        private void CreateStreamBodyRead(HttpEntityManager manager, string body)
        {
            var entity = manager.HttpEntity;

            var create = entity.RequestCodec.From<HttpClientMessageDto.CreateStreamText>(body);
            if (create == null)
            {
                SendBadRequest(entity, "Create stream request body cannot be deserialized");
                return;
            }

            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  entity,
                                                  Format.Atom.CreateStreamCompleted,
                                                  Configure.CreateStreamCompleted);
            var msg = new ClientMessage.CreateStream(Guid.NewGuid(),
                                                     envelope,
                                                     true, 
                                                     create.EventStreamId,
                                                     Guid.NewGuid(), 
                                                     false,//TODO TR discover
                                                     Encoding.UTF8.GetBytes(create.Metadata ?? string.Empty));
            Publish(msg);
        }

        public void DeleteStream(HttpEntity entity, string stream)
        {
            entity.Manager.AsyncState = stream;
            entity.Manager.ReadTextRequestAsync(DeleteStreamBodyRead,
                                            e => Log.ErrorException(e, "Error while reading request (DELETE stream)"));
        }

        private void DeleteStreamBodyRead(HttpEntityManager manager, string body)
        {
            var entity = manager.HttpEntity;
            var stream = (string)manager.AsyncState;

            var delete = entity.RequestCodec.From<HttpClientMessageDto.DeleteStreamText>(body);
            if (delete == null)
            {
                SendBadRequest(entity, "Delete stream request body cannot be deserialized");
                return;
            }

            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  entity,
                                                  Format.Atom.DeleteStreamCompleted,
                                                  Configure.DeleteStreamCompleted);
            var msg = new ClientMessage.DeleteStream(Guid.NewGuid(),
                                                     envelope,
                                                     true, 
                                                     stream, 
                                                     delete.ExpectedVersion);
            Publish(msg);
        }

        public void GetFeedPage(HttpEntity entity, string stream, int start, int count, bool embed)
        {
            entity.Manager.AsyncState = start;
            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  entity,
                                                  (ent, msg) => Format.Atom.ReadStreamEventsBackwardCompletedFeed(ent, msg, start, count, embed),
                                                  Configure.ReadStreamEventsBackwardCompleted);
            Publish(new ClientMessage.ReadStreamEventsBackward(Guid.NewGuid(), envelope, stream, start, count, resolveLinks: true));
        }

        public void GetEntry(HttpEntity entity, string stream, int version)
        {
            var envelope = new SendToHttpEnvelope(_networkSendQueue, entity, Format.Atom.ReadEventCompletedEntry, Configure.ReadEventCompleted);
            Publish(new ClientMessage.ReadEvent(Guid.NewGuid(), envelope, stream, version, true));
        }

        public void PostEntry(HttpEntity entity, string stream)
        {
            entity.Manager.AsyncState = stream;
            entity.Manager.ReadTextRequestAsync(OnPostEntryRequestRead, 
                                            e => Log.ErrorException(e, "Error while reading request (POST entry)"));
        }

        private void OnPostEntryRequestRead(HttpEntityManager manager, string body)
        {
            var entity = manager.HttpEntity;
            var stream = (string)manager.AsyncState;

            var parsed = AutoEventConverter.SmartParse(body, entity.RequestCodec);
            var expectedVersion = parsed.Item1;
            var events = parsed.Item2;

            if (events == null || events.Length == 0)
            {
                SendBadRequest(entity, "Write request body invalid");
                return;
            }

            var envelope = new SendToHttpEnvelope(_networkSendQueue, entity, Format.WriteEventsCompleted, Configure.WriteEventsCompleted);
            var msg = new ClientMessage.WriteEvents(Guid.NewGuid(), envelope, true, stream, expectedVersion, events);

            Publish(msg);
        }
    }

    public class AllEventsController : CommunicationController
    {
        private readonly IPublisher _networkSendQueue;

        public AllEventsController(IPublisher publisher, IPublisher networkSendQueue)
            : base(publisher)
        {
            _networkSendQueue = networkSendQueue;
        }

        protected override void SubscribeCore(IHttpService service, HttpMessagePipe pipe)
        {
            //no direct subscriptions
        }

        public void GetAllBefore(HttpEntity entity, TFPos position, int count)
        {
            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  entity, 
                                                  Format.Atom.ReadAllEventsBackwardCompleted, 
                                                  Configure.ReadAllEventsBackwardCompleted);
            Publish(new ClientMessage.ReadAllEventsBackward(Guid.NewGuid(),
                                                            envelope,
                                                            position.CommitPosition,
                                                            position.PreparePosition,
                                                            count,
                                                            true));
        }

        public void GetAllAfter(HttpEntity entity, TFPos position, int count)
        {
            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  entity, 
                                                  Format.Atom.ReadAllEventsForwardCompleted,
                                                  Configure.ReadAllEventsForwardCompleted);
            Publish(new ClientMessage.ReadAllEventsForward(Guid.NewGuid(),
                                                           envelope,
                                                           position.CommitPosition,
                                                           position.PreparePosition,
                                                           count,
                                                           true));
        }


    }
}