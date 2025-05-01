using blazelogBase.Store.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace blazelogBase.Store.Setup
{
    public static class InjectService
    {
        
        public static IServiceCollection AddCommandMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            //MediatR Pipelines
            services.AddScoped(
                typeof(IPipelineBehavior<,>),
                typeof(LoggingPipelineBehavior<,>));

            return services;
        }

        public static IServiceCollection AddStore(this IServiceCollection services, IConfiguration confg,string conn="BlazeLog")
        {
            services.AddScoped<IBlazeLogDbContext,BlazeLogDbContext>();

            var connc = confg.GetConnectionString(conn);

            // Add MSSQL DB Context
            services.AddDbContextFactory<BlazeLogDbContext>(options =>
            {
                options.UseSqlServer(connc, providerOptions => providerOptions.EnableRetryOnFailure()).EnableDetailedErrors();

            },lifetime: ServiceLifetime.Scoped);

            return services;
        }
    }
}
