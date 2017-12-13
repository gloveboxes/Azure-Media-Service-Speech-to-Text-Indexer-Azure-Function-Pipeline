# Azure-Media-Service-Speach-to-Text-Indexer-Azure-Function-Pipeline


## Create an Azure Active Directory Service Principle

Azure Portal -> Azure Active Directory -> App registrations

### Add New application registration

![Active Directory Create Service Princple](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speach-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/ActiveDirectoryCreateServicePrincple.JPG)

### Select the newly created service principle

![List Service Principles](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speach-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/ActiveDirectoryListIdentities.JPG)

### Record Application ID.

This is the UUID (GUID) for the Service Principle you've just created. You will need this id.

![Service Principle](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speach-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/ActiveDirectoryServicePrincipleJPG.JPG)

## Create Service Principle Key

1. From All Settings, select Keys.
2. Create a new key, select duration.
3. Record Key, this is required for Azure Functions and is only visible when created.

![settings](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speach-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/ActiveDirectoryServicePrincipleKey.JPG)




## Keys

### AMSNotificationWebHookKey

![AMSNotificationWebHookKey](https://raw.githubusercontent.com/gloveboxes/Azure-Media-Service-Speach-to-Text-Indexer-Azure-Function-Pipeline/master/Resources/WebHookKey.JPG)

