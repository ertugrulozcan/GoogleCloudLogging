using GoogleCloudLogging.Extensions.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GoogleCloudLogging.Samples.WebAPI
{
	public class Startup
	{
		#region Properties

		public IConfiguration Configuration { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="configuration"></param>
		public Startup(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		#endregion
		
		#region Methods

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddGoogleCloudLogging(this.Configuration);
			
			services
				.AddControllers()
				.AddNewtonsoftJson();
		}

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

		#endregion
	}
}