## Request/Response Logging on ASP.NET Core with Google Cloud Logging

### Getting Started

1. Create a **Google Cloud Console** account if not exist
   -> https://console.cloud.google.com/
2. Create a project if not exist
   -> https://console.cloud.google.com/cloud-resource-manager
3. Create a service account 
   -> https://console.cloud.google.com/iam-admin/serviceaccounts/create
4. Grant this service account access to project for logging administrator role
5. Download your service account key as json file 
   -> https://console.cloud.google.com/apis/credentials/serviceaccountkey
6. Paste your service account json file content to your appsettings.json file as field named of 'GoogleCloudConsole' 

### Using

#### Configuration

Paste the content of service account json file where you download from google cloud console to your appsettings.json file as field named of 'GoogleCloudConsole'. (Step 6)
Add the 'Google' section into the 'Logging' scope to your appsettings.json file for google cloud logging configuration.
Exception logging and tracing options can be switchable on this configuration.

Consider the following appsettings.json file:
#### appsettings.json
```json
{
   "Logging": {
      "LogLevel": {
         "Default": "Information",
         "Microsoft": "Warning",
         "Microsoft.Hosting.Lifetime": "Information"
      },
      "Google": {
         "Enabled": true,
         "ExceptionLogging": {
            "Enabled": false
         },
         "GoogleTrace": {
            "Enabled": false
         }
      }
   },
   "AllowedHosts": "*",
   "GoogleCloudConsole": {
      "type": "<type>",
      "project_id": "<your_project_id>",
      "private_key_id": "<your_private_key_id>",
      "private_key": "<your_private_key>",
      "client_email": "<your_client_email>",
      "client_id": "<your_client_id>",
      "auth_uri": "<auth_uri>",
      "token_uri": "<token_uri>",
      "auth_provider_x509_cert_url": "<auth_provider_x509_cert_url>",
      "client_x509_cert_url": "<client_x509_cert_url>"
   }
}
```

#### Integration

#### Startup.cs
```c#
public void ConfigureServices(IServiceCollection services)
{
    // ...
    
    services.AddGoogleCloudLogging(this.Configuration);
    
    // ...
}
```

```c#
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    
    app.UseGoogleCloudLogging();

    app.UseHttpLogging();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthorization();

    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
}
```

### Monitoring on Log Explorer

https://console.cloud.google.com/logs/

![alt text](https://i.ibb.co/FwZDFmr/Screen-Shot-2021-03-01-at-00-23-35.png "Log Explorer")