﻿using AutoMapper;
using MediatR;
using bbxBE.Application.Interfaces;
using bbxBE.Application.Interfaces.Repositories;
using bbxBE.Application.Parameters;
using bbxBE.Application.Wrappers;
using bbxBE.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using bbxBE.Application.Interfaces.Queries;
using bbxBE.Domain.Extensions;
using bbxBE.Application.Queries.ViewModels;

namespace bbxBE.Application.Queries.qUser
{
    public class QueryUser : QueryParameter, IRequest<PagedResponse<IEnumerable<Entity>>>
    {
        public string SearchString { get; set; }
    }

    public class QueryUserHandler : IRequestHandler<QueryUser, PagedResponse<IEnumerable<Entity>>>
    {
        private readonly IUserRepositoryAsync _userRepository;
        private readonly IMapper _mapper;
        private readonly IModelHelper _modelHelper;

        public QueryUserHandler(IUserRepositoryAsync userRepository, IMapper mapper, IModelHelper modelHelper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _modelHelper = modelHelper;
        }

        public async Task<PagedResponse<IEnumerable<Entity>>> Handle(QueryUser request, CancellationToken cancellationToken)
        {
            var validFilter = request;
            var pagination = request;

              /* TODO: törölni
          //filtered fields security
            if (!string.IsNullOrEmpty(validFilter.Fields))
            {
                //limit to fields in view model
                validFilter.Fields = _modelHelper.ValidateModelFields<GetUSR_USERViewModel, USR_USER>(validFilter.Fields);
            }
            if (string.IsNullOrEmpty(validFilter.Fields))
            {
                //default fields from view model
                validFilter.Fields = _modelHelper.GetQueryableFields<GetUSR_USERViewModel, USR_USER>();
            }
              */

            // query based on filter
            var entitUsers = await _userRepository.QueryPagedUserAsync(validFilter);
            var data = entitUsers.data.MapItemsFieldsByMapToAnnotation<GetUsersViewModel>();
            RecordsCount recordCount = entitUsers.recordsCount;

            // response wrapper
            return new PagedResponse<IEnumerable<Entity>>(data, validFilter.PageNumber, validFilter.PageSize, recordCount);
        }
    }
}