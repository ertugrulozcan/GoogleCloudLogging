using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Api;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using GoogleCloudLogging.Auth;
using GoogleCloudLogging.Proto;
using Microsoft.AspNetCore.Http;

namespace GoogleCloudLogging.Extensions.AspNetCore
{
	public class HttpLoggingMiddleware
	{
		#region Constants

		private const string LOG_ID = "request-response";

		#endregion
		
		#region Services

		private readonly IGoogleCloudAccount googleCloudAccount;
		private readonly LoggingServiceV2Client loggingClient;
		private readonly RequestDelegate next;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="next"></param>
		/// <param name="googleCloudAccount"></param>
		public HttpLoggingMiddleware(RequestDelegate next, IGoogleCloudAccount googleCloudAccount)
		{
			this.next = next;
			this.googleCloudAccount = googleCloudAccount;
			this.loggingClient = LoggingServiceV2Client.Create();
		}

		#endregion
		
		#region Methods

		public async Task Invoke(HttpContext context)
		{
			var request = await GetRequest(context);
			
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var response = await ExecuteRequest(context, this.next);
			stopwatch.Stop();
			var latency = stopwatch.Elapsed;
			
			var logName = new LogName(this.googleCloudAccount.ProjectId, LOG_ID);
			Value requestStruct = Helpers.ProtobufHelper.ConvertToStructValue(request);
			Value responseStruct = Helpers.ProtobufHelper.ConvertToStructValue(response);
			var resource = new MonitoredResource { Type = "global" };
			var googleCloudRequest = ConvertToGoogleCloudHttpRequest(context, latency);
			var logEntry = CreateRequestResponseLogEntry(logName, requestStruct, responseStruct, googleCloudRequest, context.Response.StatusCode < 400 ? LogSeverity.Info : LogSeverity.Error);
			await this.loggingClient.WriteLogEntriesAsync(logName, resource, null, new[] { logEntry });
		}

		private static async Task<dynamic> ExecuteRequest(HttpContext context, RequestDelegate next)
		{
			var originalBody = context.Response.Body;
			
			try
			{
				await using (var responseBodyStream = new MemoryStream())
				{
					context.Response.Body = responseBodyStream;

					await next(context);

					responseBodyStream.Seek(0, SeekOrigin.Begin);
					string responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
					responseBodyStream.Seek(0, SeekOrigin.Begin);

					await responseBodyStream.CopyToAsync(originalBody);
					
					var response = new
					{
						context.Response.StatusCode,
						Body = new JsonObject(responseBody),
						Headers = context.Request.Headers != null ? context.Request.Headers.Select(x => $"{x.Key}: {x.Value}").ToArray() : new string[0],
					};
					
					return response;
				}
			}
			finally
			{
				context.Response.Body = originalBody;
			}
		}

		private static async Task<dynamic> GetRequest(HttpContext context)
		{
			context.Request.EnableBuffering();
			string body = await new StreamReader(context.Request.Body).ReadToEndAsync();
			
			var request = new
			{
				Host = context.Request.Host.Value,
				Path = context.Request.Path.Value,
				context.Request.Method,
				context.Request.Protocol,
				context.Request.Scheme,
				QueryString = context.Request.QueryString.Value,
				Body = new JsonObject(body),
				Headers = context.Request.Headers != null ? context.Request.Headers.Select(x => $"{x.Key}: {x.Value}").ToArray() : new string[0],
				Cookies = context.Request.Cookies != null ? context.Request.Cookies.Select(x => $"{x.Key}: {x.Value}").ToArray() : new string[0],
			};

			return request;
		}

		private static LogEntry CreateRequestResponseLogEntry(
			LogName logName, 
			Value requestStruct, 
			Value responseStruct,
			Google.Cloud.Logging.Type.HttpRequest googleCloudRequest,
			LogSeverity severity = LogSeverity.Info)
		{
			return new LogEntry
			{
				LogNameAsLogName = logName,
				Severity = severity,
				JsonPayload = new Struct()
				{
					Fields =
					{
						{ "Request", requestStruct },
						{ "Response", responseStruct }
					}
				},
				HttpRequest = googleCloudRequest
			};
		}

		private static Google.Cloud.Logging.Type.HttpRequest ConvertToGoogleCloudHttpRequest(HttpContext context, TimeSpan responseTime)
		{
			var latency = Duration.FromTimeSpan(responseTime);
			var protocol = context.Request.Protocol;
			var referer = context.Request.Headers.ContainsKey("Referer") ? context.Request.Headers["Referer"].ToString() : null;
			var status = context.Response.StatusCode;
			var remoteIp = context.Connection?.RemoteIpAddress?.ToString();
			var requestMethod = context.Request.Method;
			var requestUrl = Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(context.Request);
			var serverIp = context.Connection?.LocalIpAddress?.ToString();
			var userAgent = context.Request.Headers.ContainsKey("User-Agent") ? context.Request.Headers["User-Agent"].ToString() : null;
			
			return new Google.Cloud.Logging.Type.HttpRequest
			{
				Latency = latency,
				Protocol = protocol ?? string.Empty,
				Referer = referer ?? string.Empty,
				Status = status,
				RemoteIp = remoteIp ?? string.Empty,
				RequestMethod = requestMethod ?? string.Empty,
				RequestUrl = requestUrl ?? string.Empty,
				ServerIp = serverIp ?? string.Empty,
				UserAgent = userAgent ?? string.Empty
			};
		}
		
		#endregion
	}
}