using Grpc.Core;
using Northwind.EntityModels;
using Northwind.Grpc.Service.Protos;
using Shipper = Northwind.Grpc.Service.Protos.Shipper;
using ShipperEntity = Northwind.EntityModels.Shipper;

namespace Northwind.Grpc.Service.Services
{
    public class ShipperService : Shipper.ShipperBase
    {
        private readonly ILogger<ShipperService> _logger;
        private readonly NorthwindContext _dbContext;

        public ShipperService(ILogger<ShipperService> logger, NorthwindContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public override async Task<ShipperReply> GetShipper(ShipperRequest request, ServerCallContext context)
        {
            _logger.LogCritical($"This request has a deadline of {context.Deadline:T}. It is now {DateTime.UtcNow:T}.");
            //await Task.Delay(TimeSpan.FromSeconds(5));
            var shipper = await _dbContext.Shippers.FindAsync(request.ShipperId);

            return shipper is null ? null : ToReply(shipper);
        }


        private ShipperReply ToReply(ShipperEntity shipper)
        {
            return new ShipperReply()
            {
                CompanyName = shipper.CompanyName,
                Phone = shipper.Phone,
                ShipperId = shipper.ShipperId,
            };
        }

    }
}
