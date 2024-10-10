using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Northwind.EntityModels;
using Northwind.Grpc.Service.Protos;
using Product = Northwind.Grpc.Service.Protos;

namespace Northwind.Grpc.Service.Services
{
    public class ProductService: Product.ProductBase
    {
        private const decimal NanoFactor = 1_000_000_000;
        private readonly ILogger<ProductService> _logger;
        private readonly NorthwindContext _context;
        public ProductService(NorthwindContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task<ProductReply> GetProduct(ProductRequest request, ServerCallContext context)
        {
            var product = await _context.Products.FindAsync(request.ProductId);

            return product is null ? null : ToProductReply(product);
        }

        public override async Task<ProductsReply> GetProducts(ProductsRequest request, ServerCallContext context)
        {
            var result = await _context.Products.ToListAsync();
            ProductsReply productsReply = new();
            productsReply.Products.AddRange(result.Select(ToProductReply));

            return productsReply;
        }

        public override async Task<ProductsReply> GetProductsMinimumPrice(ProductsMinimumPriceRequest request, ServerCallContext context)
        {
            var price = request.MinimumPrice.Units + (request.MinimumPrice.Nanos / NanoFactor);
            var result = await _context.Products.Where(p => p.UnitPrice > price).ToListAsync();

            ProductsReply productsReply = new();
            productsReply.Products.AddRange(result.Select(ToProductReply));

            return productsReply;
        }

        private ProductReply ToProductReply(EntityModels.Product product)
        {
            return new ProductReply()
            {
                CategoryId = product.CategoryId.Value,
                Discontinued = product.Discontinued,
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                QuantityPerUnit = product.QuantityPerUnit,
                ReorderLevel = product.ReorderLevel.Value,
                SupplierId = product.SupplierId.Value,
                UnitPrice = product.UnitPrice.HasValue ? new DecimalValue()
                {
                    Units = long.Parse(product.UnitPrice.Value.ToString().Split('.')[0]),
                    Nanos = int.Parse(product.UnitPrice.Value.ToString().Split('.')[1])
                } : null,
                UnitsInStock = product.UnitsInStock.Value,
                UnitsOnOrder = product.UnitsOnOrder.Value
            };
        }
    }
}
