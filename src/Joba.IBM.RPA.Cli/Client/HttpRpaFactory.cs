﻿using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using System.Net.Security;

namespace Joba.IBM.RPA.Cli
{
    internal static class HttpRpaFactory
    {
        private const int MaxParallelism = 10;

        public static HttpClient Create(ILogger logger, Uri address)
        {
            var handler = new ThrottlingHttpMessageHandler(MaxParallelism, CreateUserAgentHandler(logger));
            var client = new HttpClient(handler) { BaseAddress = address };
            ApplyDefaultRequestHeaders(client);
            return client;
        }

        public static HttpClient Create(ILogger logger, Uri address, IRenewExpiredSession sessionRenewal)
        {
            var refreshTokenHandler = new RefreshTokenHttpMessageHandler(sessionRenewal, CreateUserAgentHandler(logger));
            var handler = new ThrottlingHttpMessageHandler(MaxParallelism, refreshTokenHandler);
            var client = new HttpClient(handler) { BaseAddress = address };
            ApplyDefaultRequestHeaders(client);
            return client;
        }

        private static void ApplyDefaultRequestHeaders(HttpClient client)
        {
            //TODO: invalid formats
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(RpaCommand.CommandName, RpaCommand.AssemblyVersion));
            //client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(System.Environment.OSVersion.ToString()));
            //client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(System.Environment.MachineName));
        }

        private static HttpMessageHandler CreatePolicyHandler(ILogger logger)
        {
            var policy = HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(2), 5));
            var coreHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, sslPolicyErrors) =>
                {
                    var isValid = sslPolicyErrors == SslPolicyErrors.None;
                    var message = isValid ? "VALID server certificate." : $"INVALID server certificate: {sslPolicyErrors}";
                    logger.LogDebug($"{message}\n" +
                        "Requested URI: {URI}\n" +
                        "Effective date: {EffectiveDate}\n" +
                        "Expiration date: {ExpirationDate}\n" +
                        "Issuer: {certificate?.Issuer}\n" +
                        "Subject: {certificate?.Subject}",
                        requestMessage.RequestUri, certificate?.GetEffectiveDateString(), certificate?.GetExpirationDateString(), certificate?.Issuer, certificate?.Subject);

                    return isValid;
                }
            };
            return new PolicyHttpMessageHandler(policy) { InnerHandler = coreHandler };
        }

        private static HttpMessageHandler CreateUserAgentHandler(ILogger logger)
        {
            var pollyHandler = CreatePolicyHandler(logger);
            return new UserAgentHttpMessageHandler(pollyHandler);
        }
    }
}
