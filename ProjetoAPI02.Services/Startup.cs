using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjetoAPI02.DomainServices;
using ProjetoAPI02.Repository.Contexts;
using ProjetoAPI02.Repository.Interfaces;
using ProjetoAPI02.Repository.Repositories;
using ProjetoAPI02.Security.Services;
using ProjetoAPI02.Security.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoAPI02.Services
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
            services.AddControllers();

            #region Configura��o do Swagger

            services.AddSwaggerGen(
                c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "API ECommerce (NET CORE 3 e EntityFramework)",
                        Description = "API REST desenvolvida em NET CORE 3.1",
                        Version = "v1",
                        Contact = new OpenApiContact
                        {
                            Name = "COTI Inform�tica",
                            Url = new Uri("http://www.cotiinformatica.com.br/"),
                            Email = "contato@cotiinformatica.com.br"
                        }
                    });
                }
                );

            #endregion

            #region Configura��o do Reposit�rio

            services.AddDbContext<SqlServerContext>
                (options => options.UseSqlServer(Configuration.GetConnectionString("ECommerce")));

            services.AddTransient<IUnitOfWork, UnitOfWork>();

            #endregion

            #region Configura��o das classes de dominio de neg�cio

            services.AddTransient<ClienteDomainService>();
            services.AddTransient<EnderecoDomainService>();
            services.AddTransient<ProdutoDomainService>();
            services.AddTransient<PedidoDomainService>();
            services.AddTransient<ItemPedidoDomainService>();

            #endregion

            #region Configura��o da pol�tica de Autentica��o (JWT)

            //lendo o c�digo de seguran�a (Secret) contido no appsettings.json
            var settingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(settingsSection);

            var appSettings = settingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.SecretKey);

            services.AddAuthentication(
                auth =>
                {
                    auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(
                    bearer =>
                    {
                        bearer.RequireHttpsMetadata = false; //habilita o uso do HTTPS
                        bearer.SaveToken = true;
                        bearer.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateIssuer = false,
                            ValidateAudience = false
                        };
                    }
                );

            services.AddTransient(map => new TokenService(appSettings));

            #endregion

            #region Configura��o da pol�tica de CORS

            services.AddCors(
                s => s.AddPolicy("DefaultPolicy", builder => 
                {
                    //permitindo que qualquer servidor fa�a requisi��es para a API
                    builder.AllowAnyOrigin()
                    //permitindo que qualquer m�todo da API seja executado (POST, PUT, DELETE, GET etc)
                           .AllowAnyMethod()
                    //permitindo que qualquer cabe�alho seja enviado para a API (Token por exemplo)
                           .AllowAnyHeader();
                }));

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            //habilitar a pasta /wwwroot
            app.UseStaticFiles();

            //aplicando a politica CORS definida
            app.UseCors("DefaultPolicy");

            app.UseAuthentication(); //Habilitar autentica��o!
            app.UseAuthorization();

            #region Configura��o do Swagger

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Projeto API");
            });

            #endregion

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
