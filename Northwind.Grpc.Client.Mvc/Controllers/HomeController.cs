using Microsoft.AspNetCore.Mvc;
using Northwind.Grpc.Client.Mvc.Models;
using System.Diagnostics;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Google.Protobuf.WellKnownTypes;

namespace Northwind.Grpc.Client.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private const decimal NanoFactor = 1_000_000_000;
        private readonly ILogger<HomeController> _logger;
        private readonly Greeter.GreeterClient _greeterClient;
        private readonly Shipper.ShipperClient _shipperClient;
        private readonly Product.ProductClient _productClient;

        public HomeController(ILogger<HomeController> logger,GrpcClientFactory factory)
        {
            _logger = logger;
            _greeterClient = factory.CreateClient<Greeter.GreeterClient>("Greeter");
            _shipperClient = factory.CreateClient<Shipper.ShipperClient>("Shipper");
            _productClient = factory.CreateClient<Product.ProductClient>("Product");
        }

        public async Task<IActionResult> Products(decimal minimumPrice = 0M)
        {
            long units = decimal.ToInt64(minimumPrice);
            int nanos = decimal.ToInt32((minimumPrice - units) * NanoFactor);
            ProductsReply reply = await _productClient.GetProductsMinimumPriceAsync(
                new ProductsMinimumPriceRequest() { MinimumPrice = new DecimalValue()
                {
                    Nanos = nanos,
                    Units = units
                } });
            return View(reply.Products);
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
                //ShipperReply shipperReply = await _shipperClient.GetShipperAsync(
                //    new ShipperRequest { ShipperId = id });

                AsyncUnaryCall<ShipperReply> shipperCall =
                    _shipperClient.GetShipperAsync(new ShipperRequest() {ShipperId = id},
                        // Deadline must be a UTC DateTime.
                        deadline: DateTime.UtcNow.AddSeconds(3));

                var metadata = await shipperCall.ResponseHeadersAsync;
                foreach (var entry in metadata)
                {
                    // Not really critical, just doing this to make it easier to see.
                    _logger.LogCritical($"Key: {entry.Key}, Value: {entry.Value}");
                }

                var shipperReply = await shipperCall.ResponseAsync;
                model.ShipperSummary = "Shipper from gRPC service: " +
                                       $"ID: {shipperReply.ShipperId}, Name: {shipperReply.CompanyName},"
                                       + $" Phone: {shipperReply.Phone}.";
            }
            catch (RpcException rpcex) when (rpcex.StatusCode ==
                                             global::Grpc.Core.StatusCode.DeadlineExceeded)
            {
                _logger.LogWarning("Northwind.Grpc.Service deadline exceeded.");
                model.ErrorMessage = rpcex.Message;
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
