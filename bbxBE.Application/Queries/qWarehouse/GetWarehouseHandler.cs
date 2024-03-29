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

namespace bbxBE.Application.Queries.qWarehouse
{
    public class GetWarehouse:  IRequest<Entity>
    {
        public long ID { get; set; }
  //      public string Fields { get; set; }
    }

    public class GetWarehouseHandler : IRequestHandler<GetWarehouse, Entity>
    {
        private readonly IWarehouseRepositoryAsync _warehouseRepository;
        private readonly IMapper _mapper;
        private readonly IModelHelper _modelHelper;

        public GetWarehouseHandler(IWarehouseRepositoryAsync warehouseRepository, IMapper mapper, IModelHelper modelHelper)
        {
            _warehouseRepository = warehouseRepository;
            _mapper = mapper;
            _modelHelper = modelHelper;
        }

        public async Task<Entity> Handle(GetWarehouse request, CancellationToken cancellationToken)
        {
            var entity = await _warehouseRepository.GetWarehouseAsync(request.ID);
            var data = entity.MapItemFieldsByMapToAnnotation<GetWarehouseViewModel>();

            // response wrapper
            return data;
        }
    }
}