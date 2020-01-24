﻿using Newtonsoft.Json;
using System;

namespace DemoWebService
{
    public class WebSocketMessageReceivedEventArgs
    {
        [JsonProperty("Id")]
        public Guid MessageId { get; set; }

        [JsonProperty("Header")]
        public Header MessageHeader { get; set; }

        [JsonProperty("Body")]
        public string MessageBody { get; set; }
    }
    public class Header
    {
        [JsonProperty("Sender")]
        public string Sender { get; set; }

        [JsonProperty("BodyTypeName")]
        public string BodyTypeName { get; set; }
    }
}
