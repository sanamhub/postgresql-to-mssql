using Application;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddTransient<IProvider, Provider>();
services.AddTransient<IService, Service>();

var serviceProvider = services.BuildServiceProvider();
var service = serviceProvider.GetService<IService>();

service?.ValidateProviders();
service?.Migrate();
Console.ReadLine();
