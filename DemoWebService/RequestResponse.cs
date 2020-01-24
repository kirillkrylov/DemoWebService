﻿using System.Net;
namespace DemoWebService
{
    public class RequestResponse
    {
        /// <summary>
        /// HttpStatusCode
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; set; }
        public string Result { get; set; }
        public string ErrorMessage { get; set; }

    }
}
