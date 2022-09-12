using LinqKit;
using Microsoft.EntityFrameworkCore;
using bbxBE.Application.Interfaces;
using bbxBE.Application.Interfaces.Repositories;
using bbxBE.Application.Parameters;
using bbxBE.Domain.Entities;
using bbxBE.Infrastructure.Persistence.Contexts;
using bbxBE.Infrastructure.Persistence.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using bbxBE.Application.Interfaces.Queries;
using bbxBE.Application.BLL;
using System;
using AutoMapper;
using bbxBE.Application.Queries.qProduct;
using bbxBE.Application.Queries.ViewModels;
using bbxBE.Common;
using bbxBE.Common.Consts;
using bbxBE.Common.Exceptions;
using static bbxBE.Common.NAV.NAV_enums;
using bbxBE.Infrastructure.Persistence.Caches;
using Hangfire;
using System.Threading;
using EFCore.BulkExtensions;
using bbxBE.Common.Locking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace bbxBE.Infrastructure.Persistence.Repositories
{
    public class ProductRepositoryAsync : GenericRepositoryAsync<Product>, IProductRepositoryAsync
    {
        private readonly ApplicationDbContext _dbContext;
        private IDataShapeHelper<Product> _dataShaperProduct;
        private IDataShapeHelper<GetProductViewModel> _dataShaperGetProductViewModel;
        private readonly IMockService _mockData;
        private readonly IModelHelper _modelHelper;
        private readonly IMapper _mapper;
        private readonly ICacheService<Product> _productcacheService;
        private readonly ICacheService<ProductGroup> _productGroupCacheService;
        private readonly ICacheService<Origin> _originCacheService;
        private readonly ICacheService<VatRate> _vatRateCacheService;
        private List<ProductCode> pcList = new List<ProductCode>();

        /*
           "id": 2272,
   "productCode": "QQQ-RANGEX",
  "description": "QQQ range teszt átírás",
  "productGroupCode": null,
  "originCode": null,
  "unitOfMeasure": "PIECE",
  "unitPrice1": 10,
  "unitPrice2": 20,
  "latestSupplyPrice": 30,
  "isStock": true,
  "minStock": 10,
  "ordUnit":20,
  "productFee": 0,
  "active": true,
  "vtsz": "12121211",
  "ean": null,
"vatRateCode" : "27%"
        */
        public ProductRepositoryAsync(ApplicationDbContext dbContext,
            IDataShapeHelper<Product> dataShaperProduct,
            IDataShapeHelper<GetProductViewModel> dataShaperGetProductViewModel,
            IModelHelper modelHelper, IMapper mapper, IMockService mockData,
            ICacheService<Product> productCacheService,
            ICacheService<ProductGroup> productGroupCacheService,
            ICacheService<Origin> originCacheService,
            ICacheService<VatRate> vatRateCacheService
            ) : base(dbContext)
        {
            _dbContext = dbContext;

            _dataShaperProduct = dataShaperProduct;
            _dataShaperGetProductViewModel = dataShaperGetProductViewModel;
            _modelHelper = modelHelper;
            _mapper = mapper;
            _mockData = mockData;
            _productcacheService = productCacheService;
            _productGroupCacheService = productGroupCacheService;
            _originCacheService = originCacheService;
            _vatRateCacheService = vatRateCacheService;

            
            // Task.Run(() => this.RefreshProductCache_OBSOLED()).Wait();
            // Task.Run(() => this.RefreshVatRateCache_OBSOLED()).Wait();


        }

       


            public bool IsUniqueProductCode(string ProductCode, long? ProductID = null)
        {

            /*
            return !await _dbContext.ProductCode.AnyAsync(p => p.ProductCodeCategory == enCustproductCodeCategory.OWN.ToString()
                && p.ProductCodeValue.ToUpper() == ProductCode.ToUpper()
                && !p.Deleted && (ProductID == null || p.ProductID != ProductID.Value));
            */

            var query = _productcacheService.QueryCache();
            return !query.ToList().Any(p => p.ProductCodes.Any(a => a.ProductCodeCategory == enCustproductCodeCategory.OWN.ToString()
               && a.ProductCodeValue.ToUpper() == ProductCode.ToUpper())
                && !p.Deleted && (ProductID == null || p.ID != ProductID.Value));

        }


        public async Task<bool> CheckProductGroupCodeAsync(string ProductGroupCode)
        {
            if (_productGroupCacheService.IsCacheEmpty())
            {
                return await _dbContext.ProductGroup.AnyAsync(p => p.ProductGroupCode == ProductGroupCode && !p.Deleted);
            }
            else
            {
                var query = _productGroupCacheService.QueryCache();
                return query.ToList().Any(p => p.ProductGroupCode == ProductGroupCode && !p.Deleted);
            }
        }

        public async Task<bool> CheckOriginCodeAsync(string OriginCode)
        {
            if (_originCacheService.IsCacheEmpty())
            {
                return await _dbContext.Origin.AnyAsync(p => p.OriginCode == OriginCode && !p.Deleted);
            }
            else
            {
                var query = _originCacheService.QueryCache();
                return query.ToList().Any(p => p.OriginCode == OriginCode && !p.Deleted);
            }
        }

        public async Task<bool> CheckVatRateCodeAsync(string VatRateCode)
        {
            if (_vatRateCacheService.IsCacheEmpty())
            {
                return await _dbContext.VatRate.AnyAsync(p => p.VatRateCode == VatRateCode && !p.Deleted);
            }
            else
            {
                var query = _vatRateCacheService.QueryCache();
                return query.ToList().Any(p => p.VatRateCode == VatRateCode && !p.Deleted);
            }
        }

        private Product PrepareNewProduct(Product p_product, string p_ProductGroupCode, string p_OriginCode, string p_VatRateCode)
        {
            if (!string.IsNullOrWhiteSpace(p_ProductGroupCode))
            {
                ProductGroup pg;
                if (_productGroupCacheService.IsCacheEmpty())
                {
                    pg = _dbContext.ProductGroup.AsNoTracking().SingleOrDefault(x => x.ProductGroupCode == p_ProductGroupCode);
                }
                else
                {
                    var query = _productGroupCacheService.QueryCache();
                    pg = query.SingleOrDefault(x => x.ProductGroupCode == p_ProductGroupCode);
                }
                if (pg != null)
                {
                    _dbContext.Entry(pg).State = EntityState.Unchanged;
                    p_product.ProductGroupID = pg.ID;
    //                p_product.ProductGroup = pg;
                }
            }

            if (!string.IsNullOrWhiteSpace(p_OriginCode))
            {
                Origin og;
                if (_originCacheService.IsCacheEmpty())
                {
                    og = _dbContext.Origin.AsNoTracking().SingleOrDefault(x => x.OriginCode == p_OriginCode);
                }
                else
                {
                    var query = _originCacheService.QueryCache();
                   og = query.SingleOrDefault(x => x.OriginCode == p_OriginCode);
                }
                if ( og != null)
                {
                    _dbContext.Entry(og).State = EntityState.Unchanged;
                    p_product.OriginID = og.ID;
  //                  p_product.Origin = og;
                }
            }

            VatRate vr;
            if (!string.IsNullOrWhiteSpace(p_VatRateCode))
            {
                if (_vatRateCacheService.IsCacheEmpty())
                {
                    vr = _dbContext.VatRate.AsNoTracking().SingleOrDefault(x => x.VatRateCode == p_VatRateCode); 
                }
                else
                {
                    var query = _vatRateCacheService.QueryCache();
                    vr = query.SingleOrDefault(x => x.VatRateCode == p_VatRateCode);
                }

                if( vr == null) 
                {
                    vr = _dbContext.VatRate.AsNoTracking().SingleOrDefault(x => x.VatRateCode == bbxBEConsts.VATCODE_27);
                }

            }
            else
            {
                vr = _dbContext.VatRate.AsNoTracking().SingleOrDefault(x => x.VatRateCode == bbxBEConsts.VATCODE_27);
            }
   //         _dbContext.Entry(vr).State = EntityState.Unchanged;
            p_product.VatRateID = vr.ID;
//            p_product.VatRate = vr;

            foreach (var pc in p_product.ProductCodes)
            {
                pc.ProductCodeValue = pc.ProductCodeValue.ToUpper();
                pc.ProductID = pc.ID;
            }

            return p_product;
        }

        public async Task<Product> AddProductAsync(Product p_product, string p_ProductGroupCode, string p_OriginCode, string p_VatRateCode)
        {
            using (var dbContextTransaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    p_product = PrepareNewProduct(p_product, p_ProductGroupCode, p_OriginCode, p_VatRateCode);


                    await _dbContext.Product.AddAsync(p_product);

                    await _dbContext.SaveChangesAsync();

                    await dbContextTransaction.CommitAsync();
                    _productcacheService.AddOrUpdate(p_product);
                }
                catch(Exception e)
                {
                    await dbContextTransaction.RollbackAsync();
                    throw;
                }
            }
            return p_product;
        }


        public async Task<int> AddProductRangeAsync(List<Product> p_productList, List<string> p_ProductGroupCodeList, List<string> p_OriginCodeList, List<string> p_VatRateCodeList)
        {
            var item = 0;

            foreach (var prod in p_productList)
            {
                PrepareNewProduct(prod, p_ProductGroupCodeList[item], p_OriginCodeList[item], p_VatRateCodeList[item]);
                item++;
            }

            try
            {
                _dbContext.Database.SetCommandTimeout(3600);
                await _dbContext.BulkInsertAsync(p_productList, new BulkConfig { SetOutputIdentity = true, PreserveInsertOrder = true, BulkCopyTimeout = 0, WithHoldlock = false, BatchSize = 5000 });
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw e;
            }
            

            var productCodes = new List<ProductCode>();
            foreach (var product in p_productList)
            {
                foreach (var productCode in product.ProductCodes)
                {
                    productCode.ProductID = product.ID; // sets FK to match its linked PK that was generated in DB
                }
                productCodes.AddRange(product.ProductCodes);
            }
            await _dbContext.BulkInsertAsync(productCodes);
  //          await RefreshProductCache();            
            await _dbContext.SaveChangesAsync();

            return item;
        }


        private Product PrepareUpdateProduct(Product p_productUpd, string p_ProductGroupCode, string p_OriginCode, string p_VatRateCode)
        {
            Product prodOri = null;
            if (!_productcacheService.TryGetValue(p_productUpd.ID, out prodOri))
                throw new ResourceNotFoundException(string.Format(bbxBEConsts.ERR_PRODNOTFOUND, p_productUpd.ID));

            /*
            var prod = _dbContext.Product.AsNoTracking()
                        .Include(p => p.ProductCodes).AsNoTracking()
                        .Include(pg => pg.ProductGroup).AsNoTracking()
                        .Include(o => o.Origin).AsNoTracking()
                        .Include(v => v.VatRate).AsNoTracking()
                        .Where(x => x.ID == p_product.ID).FirstOrDefault();
            */


            if (prodOri != null)
            {

                if (!string.IsNullOrWhiteSpace(p_ProductGroupCode))
                {
                    ProductGroup pg = null;
                    if (_productGroupCacheService.IsCacheEmpty())
                    {

                        pg =  _dbContext.ProductGroup.AsNoTracking().SingleOrDefault(x => x.ProductGroupCode == p_ProductGroupCode);
                    }
                    else
                    {
                        var query = _productGroupCacheService.QueryCache();
                        pg = query.SingleOrDefault(x => x.ProductGroupCode == p_ProductGroupCode);
                    }
                    if (pg != null)
                    {
                        p_productUpd.ProductGroupID = pg.ID;
                        p_productUpd.ProductGroup = pg;
                    }
                }

                if (!string.IsNullOrWhiteSpace(p_OriginCode))
                {
                    Origin origin = null;

                    if (_originCacheService.IsCacheEmpty())
                    {

                        origin = _dbContext.Origin.AsNoTracking().SingleOrDefault(x => x.OriginCode == p_OriginCode);
                    }
                    else
                    {
                        var query = _originCacheService.QueryCache();
                        origin = query.SingleOrDefault(x => x.OriginCode == p_OriginCode);
                    }
                    if (origin != null)
                    {
                        p_productUpd.OriginID = origin.ID;
                        p_productUpd.Origin = origin;
                    }
                }

                if (!string.IsNullOrWhiteSpace(p_VatRateCode))
                {
                    VatRate vatRate = null;


                    if (_vatRateCacheService.IsCacheEmpty())
                    {

                        vatRate = _dbContext.VatRate.AsNoTracking().SingleOrDefault(x => x.VatRateCode == p_VatRateCode);
                    }
                    else
                    {
                        var query = _vatRateCacheService.QueryCache();
                        vatRate = query.SingleOrDefault(x => x.VatRateCode == p_VatRateCode);
                    }
                    if (vatRate != null)
                    {
                        p_productUpd.VatRateID = vatRate.ID;
                        p_productUpd.VatRate = vatRate;
                    }
                }

                //Elvileg a p_product.ProductCodes.SingleOrDefault nem nyúl DB-hez
                //

                var pc = p_productUpd.ProductCodes.SingleOrDefault(x => x.ProductCodeCategory == enCustproductCodeCategory.OWN.ToString());
                if (pc != null)
                {
                    var pcID = prodOri.ProductCodes.SingleOrDefault(x => x.ProductCodeCategory == enCustproductCodeCategory.OWN.ToString())?.ID;
                    if (pcID != null)
                        pc.ID = pcID.Value;
                }

                var vtsz = p_productUpd.ProductCodes.SingleOrDefault(x => x.ProductCodeCategory == enCustproductCodeCategory.VTSZ.ToString());
                if (vtsz != null)
                {
                    var vtszID = prodOri.ProductCodes.SingleOrDefault(x => x.ProductCodeCategory == enCustproductCodeCategory.VTSZ.ToString())?.ID;
                    if (vtszID != null)
                        vtsz.ID = vtszID.Value;
                }



                var eanOrig = prodOri.ProductCodes.SingleOrDefault(x => x.ProductCodeCategory == enCustproductCodeCategory.EAN.ToString());
                if (eanOrig != null)
                {
                    var eanUpd = p_productUpd.ProductCodes.SingleOrDefault(x => x.ProductCodeCategory == enCustproductCodeCategory.EAN.ToString());
                    if (eanUpd != null)
                        eanUpd.ID = eanOrig.ID;
                    else
                    {

                       //ez nem akar működni egyelőre nem kell
                       //_dbContext.Entry(eanOrig).State = EntityState.Deleted;

                        //_dbContext.ProductCode.Remove(eanOrig);
                    }
                }

                foreach (var pcx in p_productUpd.ProductCodes)
                {
                    pcx.ProductCodeValue = pcx.ProductCodeValue.ToUpper();
                }
            }
            else
            {
                throw new ResourceNotFoundException(string.Format(bbxBEConsts.ERR_PRODNOTFOUND, p_productUpd.ID));
            }

            return p_productUpd;
        }

        public async Task<Product> UpdateProductAsync(Product p_product, string p_ProductGroupCode, string p_OriginCode, string p_VatRateCode)
        {

            //   var manager = ((IObjectContextAdapter)_dbContext).ObjectContext.ObjectStateManager;
 
            using (var dbContextTransaction = await _dbContext.Database.BeginTransactionAsync())
            {

                p_product = PrepareUpdateProduct(p_product, p_ProductGroupCode, p_OriginCode, p_VatRateCode);



                _dbContext.Product.Update(p_product);
                await _dbContext.SaveChangesAsync();
                await dbContextTransaction.CommitAsync();
                _productcacheService.AddOrUpdate(p_product);


            }
            return p_product;
        }

        public async Task<int> UpdateProductRangeAsync(List<Product> p_productList, List<string> p_ProductGroupCodeList, List<string> p_OriginCodeList, List<string> p_VatRateCodeList)
        {

            var item = 0;

            foreach (var prod in p_productList)
            {
                PrepareUpdateProduct(prod, p_ProductGroupCodeList[item], p_OriginCodeList[item], p_VatRateCodeList[item]);
                item++;
            }

            try
            {
                _dbContext.Database.SetCommandTimeout(3600);
                await _dbContext.BulkUpdateAsync(p_productList, new BulkConfig { SetOutputIdentity = true, PreserveInsertOrder = true, BulkCopyTimeout = 0, WithHoldlock = false, BatchSize = 5000 });
                await _dbContext.SaveChangesAsync();

            }
            catch (Exception e)
            {
                throw e;
            }

            var productCodes = new List<ProductCode>();
            foreach (var product in p_productList)
            {
                foreach (var productCode in product.ProductCodes)
                {
                    productCode.ProductID = product.ID; // sets FK to match its linked PK that was generated in DB
                }
                productCodes.AddRange(product.ProductCodes);
            }
            await _dbContext.BulkUpdateAsync(productCodes);
            await RefreshProductCache();

            await _dbContext.SaveChangesAsync();

            return item;
        }

        public async Task<Product> DeleteProductAsync(long ID)
        {

            //   var manager = ((IObjectContextAdapter)_dbContext).ObjectContext.ObjectStateManager;
            Product prod = null;
            using (var dbContextTransaction = await _dbContext.Database.BeginTransactionAsync())
            {
                prod = await _dbContext.Product.Include(p => p.ProductCodes).Where(x => x.ID == ID).FirstOrDefaultAsync();

                if (prod != null)
                {

                    if (prod.ProductCodes != null)
                    {

                        _dbContext.ProductCode.RemoveRange(prod.ProductCodes.ToList());
                    }
                    _productcacheService.TryRemove(prod);

                    _dbContext.Product.Remove(prod);

                    await _dbContext.SaveChangesAsync();
                    await dbContextTransaction.CommitAsync();

                }
                else
                {
                    throw new ResourceNotFoundException(string.Format(bbxBEConsts.ERR_PRODNOTFOUND, ID));
                }
            }
            return prod;
        }

        public Product GetProduct(long ID)
        {
            var query = _productcacheService.QueryCache();
            var prod = query.Where(i => i.ID == ID).SingleOrDefault();
            return prod;
        }
        public Entity GetProductEntity(long ID)
        {

           Product prod = null;
            if (!_productcacheService.TryGetValue(ID, out prod))
                throw new ResourceNotFoundException(string.Format(bbxBEConsts.ERR_PRODNOTFOUND, ID));


            var itemModel = _mapper.Map<Product, GetProductViewModel>(prod);
            var listFieldsModel = _modelHelper.GetModelFields<GetProductViewModel>();

            // shape data
            var shapeData = _dataShaperGetProductViewModel.ShapeData(itemModel, String.Join(",", listFieldsModel));

            return shapeData;
        }

        public Product GetProductByProductCode(string productCode)
        {
            productCode = productCode.ToUpper();
            var query = _productcacheService.QueryCache();

            var prod = query.Where(i => i.ProductCodes.Any(c => c.ProductCodeValue.ToUpper() == productCode.ToUpper()
                                                                   && c.ProductCodeCategory == enCustproductCodeCategory.OWN.ToString())).FirstOrDefault();
            return prod;
        }
        public Entity GetProductEntityByProductCode(string productCode)
        {
            var prod  = GetProductByProductCode(productCode);

            //            var fields = requestParameter.Fields;
            if (prod != null)
            {
                var itemModel = _mapper.Map<Product, GetProductViewModel>(prod);
                var listFieldsModel = _modelHelper.GetModelFields<GetProductViewModel>();

                // shape data
                var shapeData = _dataShaperGetProductViewModel.ShapeData(itemModel, String.Join(",", listFieldsModel));

                return shapeData;
            }
            else
            {
                return new Entity();
            }
        }
        public async Task<(IEnumerable<Entity> data, RecordsCount recordsCount)> QueryPagedProductAsync(QueryProduct requestParameter)
        {

            var searchString = requestParameter.SearchString;

            var pageNumber = requestParameter.PageNumber;
            var pageSize = requestParameter.PageSize;
            var orderBy = requestParameter.OrderBy;

            int recordsTotal, recordsFiltered;



            var query = _productcacheService.QueryCache();

            // Count records total
            recordsTotal = query.Count();

            // filter query
            FilterBySearchString(ref query, searchString);

            // Count records after filter
            recordsFiltered = query.Count();

            //set Record counts
            var recordsCount = new RecordsCount
            {
                RecordsFiltered = recordsFiltered,
                RecordsTotal = recordsTotal
            };

            // set order by

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                if (orderBy.ToUpper() == bbxBEConsts.FIELD_PRODUCTCODE)
                {
                    //Kis heka...
                    query = query.OrderBy(o => o.ProductCodes.Single(s =>
                                s.ProductCodeCategory == enCustproductCodeCategory.OWN.ToString()).ProductCodeValue);
                }
                else
                {
                    query = query.OrderBy(orderBy);
                }

            }

            // paging
            query = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

            // retrieve data to list
            var resultData = query.ToList();

            var listFieldsModel = _modelHelper.GetModelFields<GetProductViewModel>();

            //TODO: szebben megoldani
            var resultDataModel = new List<GetProductViewModel>();
            resultData.ForEach(i => resultDataModel.Add(
                _mapper.Map<Product, GetProductViewModel>(i))
            );


            var shapeData = _dataShaperGetProductViewModel.ShapeData(resultDataModel, String.Join(",", listFieldsModel));

            return (shapeData, recordsCount);
        }

        private void FilterBySearchString(ref IQueryable<Product> p_items, string p_searchString)
        {
            if (!p_items.Any())
                return;

            if (string.IsNullOrWhiteSpace(p_searchString))
                return;

            var predicate = PredicateBuilder.New<Product>();

            var srcFor = p_searchString.ToUpper().Trim();

            predicate = predicate.And(p => p.Description.ToUpper().Contains(srcFor) ||
                    p.ProductCodes.Any(a => a.ProductCodeCategory == enCustproductCodeCategory.OWN.ToString() &&
                    a.ProductCodeValue.ToUpper().Contains(srcFor)));

            p_items = p_items.Where(predicate);
        }

        public Task<bool> SeedDataAsync(int rowCount)
        {
            throw new System.NotImplementedException();
        }

        public List<GetProductViewModel> GetAllProductsFromCache()
        {
            var resultData =  _productcacheService.QueryCache().ToList();

            //TODO: szebben megoldani
            var resultDataModel = new List<GetProductViewModel>();
            resultData.ForEach(i => resultDataModel.Add(
                _mapper.Map<Product, GetProductViewModel>(i))
            );
            return resultDataModel;
        }


        public async Task<List<Product>> GetAllProductsFromDBAsync()
        {
            return await _dbContext.Product.AsNoTracking()
                 .Include(p => p.ProductCodes).AsNoTracking()
                 .Include(pg => pg.ProductGroup).AsNoTracking()
                 .Include(o => o.Origin).AsNoTracking()
                 .Include(v => v.VatRate).AsNoTracking().ToListAsync();
        }


        public async Task RefreshProductCache()
        {
            await _productcacheService.RefreshCache();
        }

        public async Task RefreshVatRateCache_OBSOLED()
        {

            if (_vatRateCacheService.IsCacheEmpty())
            {

                var q = _dbContext.VatRate
                .AsNoTracking()
                .AsExpandable();
                await _vatRateCacheService.RefreshCache(q);
/*
                foreach (var entry in _vatRateCacheService.Cache)
                {
                    _dbContext.Entry(entry.Value).State = EntityState.Detached;
                }
*/
            }
        }
    }
}