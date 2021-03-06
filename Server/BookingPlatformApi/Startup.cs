﻿using BookingPlatform.Commom;
using BookingPlatform.Extensions;
using BookingPlatformApi.Controllers.Filter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BookingPlatformApi
{
    /// <summary>
    /// 
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// 
        /// </summary>
        public IConfiguration Configuration { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            var builder = new ConfigurationBuilder()
               .SetBasePath(env.ContentRootPath)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            this.Configuration = builder.Build();
            BaseConfigModel.SetBaseConfig(Configuration, env.ContentRootPath, env.WebRootPath);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            AddAssembly(services, "BookingTest.Service");
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(JwtAuthorizeAttribute.JwtAuthenticationScheme, x =>
            {
                var jwtConfig = new JwtAuthConfigModel();

                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, //是否验证Issuer
                    ValidateAudience = true, //是否验证Audience
                    ValidateIssuerSigningKey = true,//是否验证SecurityKey
                    //ValidateLifetime = true,//是否验证超时  当设置exp和nbf时有效 同时启用ClockSkew 
                    //ClockSkew = TimeSpan.FromSeconds(30),//注意这是缓冲过期时间，总的有效时间等于这个时间加上jwt的过期时间，如果不配置，默认是5分钟
                    //RequireExpirationTime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["JwtAuth:SecurityKey"])),//拿到SecurityKey
                    ValidIssuer = jwtConfig.Issuer,//Issuer，这两项和前面签发jwt的设置一致
                    ValidAudience = jwtConfig.Audience //Audience
                };
                x.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // 如果过期，则把<是否过期>添加到，返回头信息中
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            //注册 Swagger
            services.AddSwaggerGen(c =>
            {
                //Locate the XML file being generated by ASP.NET...
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);

                //... and tell Swagger to use those XML comments.
                c.IncludeXmlComments(xmlPath);
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v5.0.0",
                    Title = "一体化预约平台PC后端 api",
                    Description = "基于.net core 3.1  开发工具VS2019",
                    TermsOfService = new Uri("http://cnblogs.com/microfisher"),
                    Contact = new OpenApiContact { Name = "卓健科技", Email = "645302361@qq.com", Url = new Uri("http://www.zhuojianchina.com/") }
                });
                var bearerScheme = new OpenApiSecurityScheme
                {
                    Description = "在下框中输入请求头中需要添加Jwt授权Token：Bearer Token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                };
                // 开启加权小锁
                c.OperationFilter<AddResponseHeadersFilter>();
                c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
                // 在header中添加token，传递到后台
                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.AddSecurityDefinition("oauth2", bearerScheme);

            });

            //配置文件大小限制
            services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;// 60000000; 
                options.MultipartHeadersLengthLimit = int.MaxValue;
                options.ValueLengthLimit = int.MaxValue;
            });
            services.AddControllers(configure =>
           {
               configure.Filters.Add<LogFilterAttribute>();
               configure.Filters.Add<HospitalUserFilterAttribute>();//全局过滤器，不用添加特性头 

            }).AddNewtonsoftJson(options =>
            {
                //修改属性名称的序列化方式，首字母小写
                //options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();//json字符串大小写原样输出
                //修改时间的序列化方式
                options.SerializerSettings.Converters.Add(new IsoDateTimeConverter() { DateTimeFormat = "yyyy/MM/dd HH:mm:ss" });
            });


            #region CORS
            services.AddCorsSetup();
            #endregion


        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //加载配置文件
            //NLog.LogManager.LoadConfiguration("nlog.config").GetCurrentClassLogger();
            MyHttpContext.ServiceProvider = app.ApplicationServices;
            app.UseStaticFiles();

            #region Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiHelp V1");
                c.DocExpansion(DocExpansion.List);
                c.DefaultModelsExpandDepth(-1);

            });

            #endregion

            // Routing
            app.UseRouting();
            //先开启认证

            app.UseAuthentication();
            // 然后是授权中间件
            app.UseAuthorization();

            app.UseCors("LimitRequests");

            app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });

        }


        /// <summary>  
        /// 自动注册服务——获取程序集中的实现类对应的多个接口
        /// </summary>
        /// <param name="services">服务集合</param>  
        /// <param name="assemblyName">程序集名称</param>
        public void AddAssembly(IServiceCollection services, string assemblyName)
        {
            if (!String.IsNullOrEmpty(assemblyName))
            {
                Assembly assembly = Assembly.Load(assemblyName);
                List<Type> ts = assembly.GetTypes().Where(u => u.IsClass && !u.IsAbstract && !u.IsGenericType).ToList();
                foreach (var item in ts.Where(s => !s.IsInterface))
                {
                    var interfaceType = item.GetInterfaces();
                    if (interfaceType.Length == 1)
                    {
                        services.AddTransient(interfaceType[0], item);
                    }
                    if (interfaceType.Length > 1)
                    {
                        services.AddTransient(interfaceType[1], item);
                    }
                }
            }
        }

        public static class MyHttpContext
        {
            public static IServiceProvider ServiceProvider;
            public static Microsoft.AspNetCore.Http.HttpContext Current
            {
                get
                {
                    object factory = ServiceProvider.GetService(typeof(Microsoft.AspNetCore.Http.IHttpContextAccessor));
                    Microsoft.AspNetCore.Http.HttpContext context = ((Microsoft.AspNetCore.Http.HttpContextAccessor)factory).HttpContext;
                    return context;
                }
            }
        }
    }
}
