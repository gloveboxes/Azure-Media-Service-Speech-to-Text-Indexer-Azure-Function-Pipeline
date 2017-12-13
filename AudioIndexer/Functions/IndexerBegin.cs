using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace AudioIndexer.Functions
{
    public static class AudioIn
    {
        // AMS = Azure Media Services
        private static CloudMediaContext _context = null;
        static string _AMSstorageAccountName = ConfigurationManager.AppSettings["AMSStorageAccountName"];
        static string _AMSstorageAccountKey = ConfigurationManager.AppSettings["AMSStorageAccountKey"];
        static string _AMSNotificationWebHookUri = ConfigurationManager.AppSettings["AMSNotificationWebHookUri"];
        static string _AMSNotificationWebHookKey = ConfigurationManager.AppSettings["AMSNotificationWebHookKey"];
        static string _AMSRESTAPIEndpoint = ConfigurationManager.AppSettings["AMSRESTAPIEndpoint"];
        static string _azureTenantId = ConfigurationManager.AppSettings["AzureTenantId"];
        static string _azureClientId = ConfigurationManager.AppSettings["AzureClientId"];
        static string _azureClientKey = ConfigurationManager.AppSettings["AzureClientKey"];


        static string configurationFile = "";

        [FunctionName("IndexerBegin")]
        public static void Run([BlobTrigger("audio-in/{name}", Connection = "AudioInConnectionString")]CloudBlockBlob inputBlob, string name, TraceWriter log)
        {
            log.Info($"Azure Media Services Audio Indexer for '{name}' started");

            try
            {
                AzureAdTokenCredentials tokenCredentials = new AzureAdTokenCredentials(_azureTenantId,
                                    new AzureAdClientSymmetricKey(_azureClientId, _azureClientKey),
                                    AzureEnvironments.AzureCloudEnvironment);

                AzureAdTokenProvider tokenProvider = new AzureAdTokenProvider(tokenCredentials);
                _context = new CloudMediaContext(new Uri(_AMSRESTAPIEndpoint), tokenProvider);

                // Step 1:  Copy the Blob into a new Input Asset for the Job
                // ***NOTE: Ideally we would have a method to ingest a Blob directly here somehow. 
                // using code from this sample - https://azure.microsoft.com/en-us/documentation/articles/media-services-copying-existing-blob/

                StorageCredentials mediaServicesStorageCredentials = new StorageCredentials(_AMSstorageAccountName, _AMSstorageAccountKey);

                CopyBlobHelpers cbh = new CopyBlobHelpers(_context, _AMSstorageAccountName, _AMSstorageAccountKey, null);

                IAsset newAsset = cbh.CreateAssetFromBlob(inputBlob, name, log).GetAwaiter().GetResult();
                log.Info("Deleting the source asset from the input container");
                inputBlob.DeleteIfExists();

                byte[] keyBytes = Convert.FromBase64String(_AMSNotificationWebHookKey);

                var endpoint = _context.NotificationEndPoints.Create("FunctionWebHook", NotificationEndPointType.WebHook, _AMSNotificationWebHookUri, keyBytes);

                IJob job = _context.Jobs.Create("Indexing: " + name);

                string MediaProcessorName = "Azure Media Indexer";  // Get a reference to the Azure Media Indexer.
                IMediaProcessor processor = GetLatestMediaProcessorByName(MediaProcessorName);

                // Read configuration from file if specified.
                string configuration = string.IsNullOrEmpty(configurationFile) ? "" : File.ReadAllText(configurationFile);

                ITask task = job.Tasks.AddNew("Audio Indexing Task",
                    processor,
                    configuration,
                    TaskOptions.None);

                task.InputAssets.Add(newAsset);
                task.OutputAssets.AddNew("Output Asset", AssetCreationOptions.None);
                task.TaskNotificationSubscriptions.AddNew(NotificationJobState.All, endpoint, true);

                job.Submit();
            }
            catch (Exception ex)
            {
                log.Info("Exception: " + ex.Message);
            }
        }

        static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = _context.MediaProcessors
            .Where(p => p.Name == mediaProcessorName)
            .ToList()
            .OrderBy(p => new Version(p.Version))
            .LastOrDefault();

            if (processor == null)
            {
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));
            }

            return processor;
        }
    }
}