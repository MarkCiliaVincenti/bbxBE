﻿//using bbxBE.Application.Features.Positions.Queries.GetPositions;
using bbxBE.Application.Interfaces;
using bbxBE.Application.Interfaces.Queries;
using bbxBE.Application.Parameters;
using bbxBE.Application.Queries.qProduct;
using bbxBE.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace bbxBE.Application.Interfaces.Repositories
{
    public interface IProductRepositoryAsync : IGenericRepositoryAsync<Product>
    {
        Task<bool> IsUniqueProductCodeAsync(string ProductCode, long? ID = null);
        Task<bool> CheckProductGroupCodeAsync(string ProductGroupCode);
        Task<bool> CheckOriginCodeAsync(string OriginCode);
        Task<Product> AddProductAsync(Product p_product, string p_ProductGroupCode, string p_OriginCode, string p_VatRateCode);
        Task<int> AddProductRangeAsync(List<Product> p_productList, List<string> p_ProductGroupCodeList, List<string> p_OriginCodeList, List<string> p_VatRateCodeList);
        Task<Product> UpdateProductAsync(Product p_product, string p_ProductGroupCode, string p_OriginCode, string p_VatRateCode);
        Task<int> UpdateProductRangeAsync(List<Product> p_productList, List<string> p_ProductGroupCodeList, List<string> p_OriginCodeList, List<string> p_VatRateCodeList);
        Task<Product> DeleteProductAsync(long ID);

        Task<bool> SeedDataAsync(int rowCount);
        Task<Entity> GetProductAsync(GetProduct requestParameters);
        Task<Entity> GetProductByProductCodeAsync(GetProductByProductCode requestParameter);
        Task<(IEnumerable<Entity> data, RecordsCount recordsCount)> QueryPagedProductAsync(QueryProduct requestParameters);
        Task<List<Product>> GetAllProductAsync();

    }
}