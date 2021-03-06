﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EsiNet.Logging;
using EsiNet.Pipeline;

namespace EsiNet.Http
{
    public class HttpLoader : IHttpLoader
    {
        private readonly HttpClientFactory _httpClientFactory;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly Log _log;
        private readonly IReadOnlyCollection<IHttpLoaderPipeline> _pipelines;

        public HttpLoader(
            HttpClientFactory httpClientFactory,
            HttpRequestMessageFactory httpRequestMessageFactory,
            IEnumerable<IHttpLoaderPipeline> pipelines,
            Log log)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpRequestMessageFactory = httpRequestMessageFactory ?? throw new ArgumentNullException(nameof(httpRequestMessageFactory));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _pipelines = pipelines?.Reverse().ToArray() ?? throw new ArgumentNullException(nameof(pipelines));
        }

        public async Task<HttpResponseMessage> Get(Uri uri, EsiExecutionContext executionContext)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            try
            {
                var response = await Execute(uri, executionContext);
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (Exception ex)
            {
                _log.Error(() => $"Error when loading '{uri}'.", ex);
                throw;
            }
        }

        private Task<HttpResponseMessage> Execute(Uri uri, EsiExecutionContext executionContext)
        {
            Task<HttpResponseMessage> Send(Uri u, EsiExecutionContext ec) => ExecuteRequest(uri, executionContext);

            return _pipelines
                .Aggregate(
                    (HttpLoadDelegate) Send,
                    (next, pipeline) => async (u, ec) => await pipeline.Handle(u, ec, next))(uri, executionContext);
        }

        private Task<HttpResponseMessage> ExecuteRequest(Uri uri, EsiExecutionContext executionContext)
        {
            var request = _httpRequestMessageFactory(uri, executionContext);

            var httpClient = _httpClientFactory(uri);
            return httpClient.SendAsync(request);
        }
    }
}