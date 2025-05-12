using Application.Providers;
using Application.Providers.Interfaces;
using Application.Services;
using Application.Services.Interfaces;
using Application.Validators;
using Application.Validators.Interfaces;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddTransient<IProvider, Provider>();
services.AddTransient<IValidator, Validator>();
services.AddTransient<IService, Service>();

var serviceProvider = services.BuildServiceProvider();
var service = serviceProvider.GetService<IService>();

service?.Migrate();
Console.ReadLine();
