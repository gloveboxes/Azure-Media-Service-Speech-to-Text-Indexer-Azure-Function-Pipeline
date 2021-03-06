# Azure Media Service Speech to Text Indexer Azure Function Pipeline

## Solution Overview

In summary, an audio asset is added to Azure Blob Storage, a Blob Storage trigger Azure Function copies the audio file to Azure Media Services Blob Storage and initiates a Media Analytics Indexer Job. On completion of the job an Webhook function is called and the job metadata is queued for consumption by the next process in the pipeline.

![Solution Overview](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speech-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/Media%20Indexer.jpg)


# Getting Started Resources

1. [Indexing Media Files with Azure Media Indexer](https://docs.microsoft.com/en-us/azure/media-services/media-services-index-content)
2. [Media Analytics on the Media Services platform](https://docs.microsoft.com/en-us/azure/media-services/media-services-analytics-overview)
3. [Use Azure AD authentication to access Azure Media Services API with .NET](https://docs.microsoft.com/en-us/azure/media-services/media-services-dotnet-get-started-with-aad)
4. [Develop Azure Functions with Media Services](https://docs.microsoft.com/en-us/azure/media-services/media-services-dotnet-how-to-use-azure-functions)
5. [Integrating applications with Azure Active Directory](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-integrating-applications)


# Azure Function Application Settings

The following keys pairs are required by the solution.

|Key|Value|
|--|--|
|AudioInConnectionString| Azure Storage connection string of source audio files |
|AMSStorageAccountName|Azure Media Services storage account name|
|AMSStorageAccountKey| Azure Media Services storage account key|
|AMSNotificationWebHookUri| From the Azure Function IndexerCompleted blade click **Get function URL** ![Get function URL](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speech-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/AzureFunctionGetFunctionURL.JPG)|
|AMSNotificationWebHookKey| The default key of the Azure IndexerCompleted Function ![AMSNotificationWebHookKey](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speach-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/WebHookKey.JPG)|
|AMSRESTAPIEndpoint| The Azure Media Services REST API Endpoint (Media Services Overview Tab) ![Media Sercices REST API Endpoint](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speech-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/AzureMediaServicesRESTAPIEndpoint.JPG)|
|AzureTenantId|Specify your Azure AD tenant domain, for example "microsoft.onmicrosoft.com" or the Directory ID (Active Directory -> Properties)|
|AzureClientId| Client ID UUID of the Active Directory service principal you created for the Azure Function to authenticate with Media Services (See notes below on how to create)|
|AzureClientSecret| The key of the Active Directory service principal you created for the Azure Function to authenticate with Media Services (See notes below on how to create)

# Azure Services Required

1. [Active Directory](https://azure.microsoft.com/en-us/services/active-directory/)
2. [Media Analytics](https://azure.microsoft.com/en-us/services/media-services/media-analytics/)
2. [Function App](https://azure.microsoft.com/en-us/services/functions/) Publish code from Visual Studio to this Function App
4. [Storage Account](https://docs.microsoft.com/en-us/azure/storage/) Configured as follows:-

    * Blob container named 'audio-in'. This is the container that the Azure Blob Storage Trigger Function monitors
    * Queue named 'processed-audio'. This is where the output metadata for a indexer job is queued.


## Create an Azure Active Directory Service Principal

When you're using Azure AD authentication with Azure Media Services, you can authenticate in one of two ways:

1. User authentication authenticates a person who is using the app to interact with Azure Media Services resources. The interactive application should first prompt the user for credentials. An example is a management console app that's used by authorized users to monitor encoding jobs or live streaming. 
2. Service principal authentication authenticates a service. Applications that commonly use this authentication method are apps that run daemon services, middle-tier services, or scheduled jobs, such as web apps, function apps, logic apps, APIs, or microservices.


**For Azure Media Services integrated with Azure Functions you need to create a service principal**

See [Get started with Azure AD authentication by using the Azure portal](
https://docs.microsoft.com/en-us/azure/media-services/media-services-portal-get-started-with-aad)

Azure Portal -> Azure Active Directory -> App registrations

### STEP 1: Add New application registration

![Active Directory Create Service Principal](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speech-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/ActiveDirectoryCreateServicePrincipal.JPG)

### STEP 2: Select the newly created Service Principal

![List Service Principals](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speach-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/ActiveDirectoryListIdentities.JPG)

### STEP 3: Copy the Display Name and the Application ID (aka Client ID).

You'll need both the Display Name and the Application ID (aka Client ID) [UUID](https://en.wikipedia.org/wiki/Universally_unique_identifier) for the Service Principal you've just created. 

You'll need the:-

1. Application ID (aka Client ID): You will need this id when configuring the Azure Function 'IndexerBegin' **AzureClientId** Application setting.
2. Display Name: You'll need this name when connecting this service principal to Azure Media Services.

![Service Principal](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speach-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/ActiveDirectoryServicePrincipalJPG.JPG)

## STEP 4: Create Service Principal Key

1. From All Settings, select Keys.
2. Create a new key, select duration.
3. Copy this Key, this is required for Azure Functions **AzureClientSecret** application setting and is only visible when created.

![settings](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speach-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/ActiveDirectoryServicePrincipalKey.JPG)


## STEP 5: Associate Service Principal with Media Services

1. From Azure Media Services blade select API Access then **Azure Media Services API with service principal**.![Azure Media Services API with service principal](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speech-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/Media%20Services%20API%20access.JPG)
2. In the search box type in the display name of the service principal in Active Directory you created in step 1, select, then grant. ![](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speech-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/Connect%20Media%20Services%20API%20with%20Service%20Principal.JPG)

