﻿using bbxBE.Application.Interfaces;
using bbxBE.Application.Interfaces.Queries;
using bbxBE.Application.Parameters;
using bbxBE.Application.Queries.qUser;
using bbxBE.Application.Queries.ViewModels;
using bbxBE.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace bbxBE.Application.Interfaces.Repositories
{
    public interface IUserRepositoryAsync : IGenericRepositoryAsync<Users>
    {
        Task<bool> IsUniqueNameAsync(string Name, long? ID = null);

        Task<bool> SeedDataAsync(int rowCount);

        Task<Entity> GetUserAsync(GetUser requestParameters);
        Task<(IEnumerable<Entity> data, RecordsCount recordsCount)> QueryPagedUserAsync(QueryUser requestParameters);
    }
}