using L4NdoStreamService.Entities;
using L4NdoStreamService.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace L4NdoStreamService
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(o => o.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));
            services.AddSingleton(new ConcurrentDictionary<string, WebRtcRenderer>(new Dictionary<string, WebRtcRenderer>
            {
                { "basler", null },
                { "emulator", null },
                { "ids", null },
                { "image", null },
            }));
            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime ltm, ConcurrentDictionary<string, WebRtcRenderer> renderers)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            ltm.ApplicationStopping.Register(() =>
            {
                foreach(WebRtcRenderer renderer in renderers.Values)
                {
                    renderer.Dispose();
                }
            });

            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseRouting();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<LiveStreamHub>("/livestream");
            });
        }
    }
}
