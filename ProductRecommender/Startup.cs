using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ML;
using Microsoft.ML;
using ProductRecommender.Database;
using ProductRecommender.Model;
using ProductRecommender.Models;

namespace ProductRecommender
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
               options.AddDefaultPolicy(builder =>
               {
                   builder.AllowAnyHeader();
                   builder.AllowAnyMethod();
                   builder.AllowAnyOrigin();
               } ); 
            });
            string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            string modelHost = Configuration["MODEL_HOST"];
            if (connectionString == null)
            {
                connectionString = Configuration.GetConnectionString("CONNECTION_STRING");
                Environment.SetEnvironmentVariable("CONNECTION_STRING",connectionString);
            }
            services.AddDbContext<ProductDbContext>(options =>
                // options.UseSqlite(connectionString)
                options.UseNpgsql(connectionString)
                 );
            services.AddControllers(options =>
                {
                    options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
                })
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

            // HACK
            // var modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "ProductRecommenderModel.zip");
            // services.AddTransient<MLContext>();
            
            var mlModel = new ModelHolder();
            DataViewSchema modelSchema;
            var _mlContext = new MLContext();
            (IDataView trainingDataView, IDataView testDataView) = MlModel.LoadData(_mlContext);
            ITransformer trainedModel = MlModel.BuildAndTrainModel(_mlContext, trainingDataView);
            mlModel.Model = trainedModel;
            services.AddSingleton(mlModel);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHttpsRedirection();
            app.UseCors();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }

    public class SlugifyParameterTransformer : IOutboundParameterTransformer
    {
        public string TransformOutbound(object value)
        {
            // Slugify value
            return value == null ? null : Regex.Replace(value.ToString(), "([a-z])([A-Z])", "$1-$2").ToLower();
        }
    }
}