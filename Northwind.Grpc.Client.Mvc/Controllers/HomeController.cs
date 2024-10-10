using Microsoft.AspNetCore.Mvc;
using Northwind.Grpc.Client.Mvc.Models;
using System.Diagnostics;
using Grpc.Net.ClientFactory;

namespace Northwind.Grpc.Client.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Greeter.GreeterClient _greeterClient;
        private readonly Shipper.ShipperClient _shipperClient;
        public HomeController(ILogger<HomeController> logger,GrpcClientFactory factory)
        {
            _logger = logger;
            _greeterClient = factory.CreateClient<Greeter.GreeterClient>("Greeter");
            _shipperClient = factory.CreateClient<Shipper.ShipperClient>("Shipper");
        }

        public async Task<IActionResult> Index(string name = "Ashkan",int id = 1)
        {
            HomeIndexViewModel model = new();
            try
            {
                var result = await _greeterClient.SayHelloAsync(new HelloRequest()
                {
                    Name = name
                });
                model.Greeting = "Greeting from gRPC service: " + result.Message;
                ShipperReply shipperReply = await _shipperClient.GetShipperAsync(
                    new ShipperRequest { ShipperId = id });
                model.ShipperSummary = "Shipper from gRPC service: " +
                                       $"ID: {shipperReply.ShipperId}, Name: {shipperReply.CompanyName},"
                                       + $" Phone: {shipperReply.Phone}.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Northwind.Grpc.Service is not responding.");
                model.ErrorMessage = ex.Message;
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
