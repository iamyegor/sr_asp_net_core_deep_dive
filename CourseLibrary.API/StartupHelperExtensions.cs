﻿using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Helpers.PropertyMapping;
using CourseLibrary.API.Services;
using CourseLibrary.API.Validations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

namespace CourseLibrary.API;

internal static class StartupHelperExtensions
{
    // Add services to the container
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder
            .Services.AddControllers(configure =>
            {
                configure.ReturnHttpNotAcceptable = true;
            })
            .AddNewtonsoftJson(appBuilder =>
            {
                appBuilder.SerializerSettings.ContractResolver =
                    new CamelCasePropertyNamesContractResolver();
            })
            .AddXmlDataContractSerializerFormatters()
            .ConfigureApiBehaviorOptions(setupAction =>
            {
                setupAction.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetailsFactory =
                        context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

                    var validationProblemDetails =
                        problemDetailsFactory.CreateValidationProblemDetails(
                            context.HttpContext,
                            context.ModelState
                        );

                    validationProblemDetails.Instance = context.HttpContext.Request.Path;
                    validationProblemDetails.Status = StatusCodes.Status422UnprocessableEntity;

                    return new UnprocessableEntityObjectResult(validationProblemDetails)
                    {
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });

        builder.Services.AddScoped<ICourseLibraryRepository, CourseLibraryRepository>();

        builder.Services.AddDbContext<CourseLibraryContext>(options =>
        {
            options.UseSqlite(@"Data Source=library.db");
        });

        builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        builder.Services.AddValidatorsFromAssemblyContaining<IValidatorsAssembly>();
        builder.Services.AddFluentValidationAutoValidation(options =>
        {
            options.OverrideDefaultResultFactoryWith<CustomResultFactory>();
        });

        builder.Services.AddTransient<PropertyMappingService>();

        builder.Services.AddTransient<PropertyChecker>();

        builder.Services.AddResponseCaching();
        builder.Services.AddHttpCacheHeaders(
            expirationModel =>
            {
                expirationModel.MaxAge = 120;
            },
            validationModel =>
            {
                validationModel.MustRevalidate = true;
            }
        );

        return builder.Build();
    }

    // Configure the request/response pipelien
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler(appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("An internal server error occured");
                });
            });
        }

        app.UseResponseCaching();

        app.UseHttpCacheHeaders();

        app.UseAuthorization();

        app.MapControllers();

        return app;
    }

    public static async Task ResetDatabaseAsync(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var context = scope.ServiceProvider.GetService<CourseLibraryContext>();
                if (context != null)
                {
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }
    }
}
