using System;
using System.IO;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using GoogleCloudLogging.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoogleCloudLogging.Extensions.AspNetCore
{
	public static class GoogleStackDriverExtensions
	{
		#region Methods

		public static void AddGoogleCloudLogging(this IServiceCollection services, IConfiguration configuration)
		{
			if (configuration.GetSection("Logging").GetSection("Google").GetValue<bool>("Enabled"))
			{
				var googleCloudConfiguration = GetGoogleCloudAccountInfo(configuration);
				services.AddSingleton(googleCloudConfiguration);
				SetGoogleAuthentication(googleCloudConfiguration);

				if (configuration.GetSection("Logging:Google:ExceptionLogging").GetValue<bool>("Enabled"))
				{
					services.AddGoogleExceptionLogging(options =>
					{
						options.ProjectId = configuration["GoogleCloudConsole:project_id"];
					});	
				}
				
				if (configuration.GetSection("Logging:Google:GoogleTrace").GetValue<bool>("Enabled"))
				{
					services.AddGoogleTrace(options =>
					{
						options.ProjectId = configuration["GoogleCloudConsole:project_id"];
						options.Options = TraceOptions.Create(bufferOptions: BufferOptions.NoBuffer());
					});		
				}
			}
		}

		public static void UseGoogleCloudLogging(this IApplicationBuilder app)
		{
			var configuration = app.ApplicationServices.GetService<IConfiguration>();
			
			if (configuration.GetSection("Logging").GetSection("Google").GetValue<bool>("Enabled"))
			{
				var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
				loggerFactory.AddGoogle(app.ApplicationServices, configuration["GoogleCloudConsole:project_id"]);

				if (configuration.GetSection("Logging:Google:ExceptionLogging").GetValue<bool>("Enabled"))
				{
					app.UseGoogleExceptionLogging();	
				}
				
				if (configuration.GetSection("Logging:Google:GoogleTrace").GetValue<bool>("Enabled"))
				{
					app.UseGoogleTrace();
				}
			}
		}

		private static IGoogleCloudAccount GetGoogleCloudAccountInfo(IConfiguration configuration)
		{
			var googleCloudConfiguration = configuration.GetSection("GoogleCloudConsole");
			return new GoogleCloudAccount
			{
				Type = googleCloudConfiguration.GetValue<string>("type"),
				ProjectId = googleCloudConfiguration.GetValue<string>("project_id"),
				PrivateKeyId = googleCloudConfiguration.GetValue<string>("private_key_id"),
				PrivateKey = googleCloudConfiguration.GetValue<string>("private_key"),
				ClientEmail = googleCloudConfiguration.GetValue<string>("client_email"),
				ClientId = googleCloudConfiguration.GetValue<string>("client_id"),
				AuthUri = googleCloudConfiguration.GetValue<string>("auth_uri"),
				TokenUri = googleCloudConfiguration.GetValue<string>("token_uri"),
				AuthProviderX509CertUrl = googleCloudConfiguration.GetValue<string>("auth_provider_x509_cert_url"),
				ClientX509CertUrl = googleCloudConfiguration.GetValue<string>("client_x509_cert_url"),
			};
		}
		
		private static void SetGoogleAuthentication(IGoogleCloudAccount googleCloudServiceAccountInfo)
		{
			var jsonCredentials = Newtonsoft.Json.JsonConvert.SerializeObject(googleCloudServiceAccountInfo);

			var credentialsFilePath = $"{Path.GetTempPath()}/google-application-credentials.json";
			using (var streamWriter = File.CreateText(credentialsFilePath))
			{
				streamWriter.Write(jsonCredentials);
			}
			
			Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsFilePath);
		}
		
		#endregion
	}
}