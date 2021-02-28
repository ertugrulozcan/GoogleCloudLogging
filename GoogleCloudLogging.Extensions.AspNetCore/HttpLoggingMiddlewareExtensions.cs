using Microsoft.AspNetCore.Builder;

namespace GoogleCloudLogging.Extensions.AspNetCore
{
	public static class HttpLoggingMiddlewareExtensions
	{
		#region Methods

		public static IApplicationBuilder UseHttpLogging(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<HttpLoggingMiddleware>();
		}

		#endregion
	}
}