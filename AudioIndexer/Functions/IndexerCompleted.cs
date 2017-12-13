using AudioIndexer.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.MediaServices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace AudioIndexer.Functions
{
    public static class IndexerCompleted
    {
        static string _azureTenantId = ConfigurationManager.AppSettings["AzureTenantId"];
        static string _azureClientId = ConfigurationManager.AppSettings["AzureClientId"];
        static string _azureClientKey = ConfigurationManager.AppSettings["AzureClientKey"];
        static string _AMSRESTAPIEndpoint = ConfigurationManager.AppSettings["AMSRESTAPIEndpoint"];
        static string _AMSNotificationWebHookKey = ConfigurationManager.AppSettings["AMSNotificationWebHookKey"];

        static string SignatureHeaderKey = "sha256";
        static string SignatureHeaderValueTemplate = SignatureHeaderKey + "={0}";
        static CloudMediaContext _context = null;

        static readonly char[] HexLookup = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        [FunctionName("IndexerCompleted")]

        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, [Queue("processed-audio", Connection = "AudioInConnectionString")]ICollector<AssetInfo> assetQueueItem, TraceWriter log)
        {
            Task<byte[]> taskForRequestBody = req.Content.ReadAsByteArrayAsync();
            byte[] requestBody = await taskForRequestBody;

            string jsonContent = await req.Content.ReadAsStringAsync();

            IEnumerable<string> values = null;
            if (req.Headers.TryGetValues("ms-signature", out values))
            {
                byte[] signingKey = Convert.FromBase64String(_AMSNotificationWebHookKey);
                string signatureFromHeader = values.FirstOrDefault();

                if (VerifyWebHookRequestSignature(requestBody, signatureFromHeader, signingKey))
                {
                    string requestMessageContents = Encoding.UTF8.GetString(requestBody);
                    NotificationMessage msg = JsonConvert.DeserializeObject<NotificationMessage>(requestMessageContents);

                    if (VerifyHeaders(req, msg, log))
                    {
                        string newJobStateStr = (string)msg.Properties.Where(j => j.Key == "NewState").FirstOrDefault().Value;
                        if (newJobStateStr == "Finished")
                        {
                            log.Info("Finished Notification Received");
                            AzureAdTokenCredentials tokenCredentials = new AzureAdTokenCredentials(_azureTenantId,
                                        new AzureAdClientSymmetricKey(_azureClientId, _azureClientKey),
                                        AzureEnvironments.AzureCloudEnvironment);

                            AzureAdTokenProvider tokenProvider = new AzureAdTokenProvider(tokenCredentials);
                            _context = new CloudMediaContext(new Uri(_AMSRESTAPIEndpoint), tokenProvider);

                            if (_context != null)
                            {
                                var assetInfo = GatherMediaAssets(msg.Properties["JobId"]);
                                assetQueueItem.Add(assetInfo);
                                log.Info($"Processing completed for {assetInfo.InputFilename}");
                            }
                        }
                        return req.CreateResponse(HttpStatusCode.OK, string.Empty);
                    }
                    else
                    {
                        log.Info($"VerifyHeaders failed.");
                        return req.CreateResponse(HttpStatusCode.BadRequest, "VerifyHeaders failed.");
                    }
                }
                else
                {
                    log.Info($"VerifyWebHookRequestSignature failed.");
                    return req.CreateResponse(HttpStatusCode.BadRequest, "VerifyWebHookRequestSignature failed.");
                }
            }
            return req.CreateResponse(HttpStatusCode.BadRequest, "Generic Error.");
        }

        private static bool VerifyHeaders(HttpRequestMessage req, NotificationMessage msg, TraceWriter log)
        {
            bool headersVerified = false;

            try
            {
                IEnumerable<string> values = null;
                if (req.Headers.TryGetValues("ms-mediaservices-accountid", out values))
                {
                    string accountIdHeader = values.FirstOrDefault();
                    string accountIdFromMessage = msg.Properties["AccountId"];

                    if (0 == string.Compare(accountIdHeader, accountIdFromMessage, StringComparison.OrdinalIgnoreCase))
                    {
                        headersVerified = true;
                    }
                    else
                    {
                        log.Info($"accountIdHeader={accountIdHeader} does not match accountIdFromMessage={accountIdFromMessage}");
                    }
                }
                else
                {
                    log.Info($"Header ms-mediaservices-accountid not found.");
                }
            }
            catch (Exception e)
            {
                log.Info($"VerifyHeaders hit exception {e}");
                headersVerified = false;
            }

            return headersVerified;
        }

        private static AssetInfo GatherMediaAssets(String jobID)
        {
            AssetInfo ai = new AssetInfo();

            IJob job = _context.Jobs.Where(j => j.Id == jobID).FirstOrDefault();
            IAsset outputAssets = job.OutputMediaAssets.FirstOrDefault();
            IAsset inputAssets = job.InputMediaAssets.FirstOrDefault();

            ai.InputBlobContainer = inputAssets.Uri;
            ai.InputFilename = inputAssets.Name;
            ai.OutputBlobContainer = outputAssets.Uri;
            ai.OutputCloseCaptionTTML = outputAssets.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".ttml")).FirstOrDefault().Name;
            ai.OutputCloseCaptionVTT = outputAssets.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".vtt")).FirstOrDefault().Name;

            return ai;
        }

        private static bool VerifyWebHookRequestSignature(byte[] data, string actualValue, byte[] verificationKey)
        {
            using (var hasher = new HMACSHA256(verificationKey))
            {
                byte[] sha256 = hasher.ComputeHash(data);
                string expectedValue = string.Format(CultureInfo.InvariantCulture, SignatureHeaderValueTemplate, ToHex(sha256));

                return (0 == String.Compare(actualValue, expectedValue, System.StringComparison.Ordinal));
            }
        }


        /// <summary>
        /// Converts a <see cref="T:byte[]"/> to a hex-encoded string.
        /// </summary>
        private static string ToHex(byte[] data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            char[] content = new char[data.Length * 2];
            int output = 0;
            byte d;

            for (int input = 0; input < data.Length; input++)
            {
                d = data[input];
                content[output++] = HexLookup[d / 0x10];
                content[output++] = HexLookup[d % 0x10];
            }

            return new string(content);
        }
    }

    internal sealed class NotificationMessage
    {
        public string MessageVersion { get; set; }
        public string ETag { get; set; }
        public NotificationEventType EventType { get; set; }
        public DateTime TimeStamp { get; set; }
        public IDictionary<string, string> Properties { get; set; }
    }

    internal enum NotificationEventType
    {
        None = 0,
        JobStateChange = 1,
        NotificationEndPointRegistration = 2,
        NotificationEndPointUnregistration = 3,
        TaskStateChange = 4,
        TaskProgress = 5
    }
}
