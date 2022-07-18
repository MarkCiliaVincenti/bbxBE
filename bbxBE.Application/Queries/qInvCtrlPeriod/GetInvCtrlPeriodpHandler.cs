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

namespace bbxBE.Application.Queries.qInvCtrlPeriod
{
    public class GetInvCtrlPeriod:  IRequest<Entity>
    {
        public long ID { get; set; }
    }

    public class GetInvCtrlPeriodHandler : IRequestHandler<GetInvCtrlPeriod, Entity>
    {
        private readonly IInvCtrlPeriodRepositoryAsync _positionRepository;
        private readonly IMapper _mapper;
        private readonly IModelHelper _modelHelper;

        public GetInvCtrlPeriodHandler(IInvCtrlPeriodRepositoryAsync positionRepository, IMapper mapper, IModelHelper modelHelper)
        {
            _positionRepository = positionRepository;
            _mapper = mapper;
            _modelHelper = modelHelper;
        }

        public async Task<Entity> Handle(GetInvCtrlPeriod request, CancellationToken cancellationToken)
        {
            var validFilter = request;
            var pagination = request;
          

            // query based on filter
            var entityPositions = await _positionRepository.GetInvCtrlPeriodAsync(validFilter);
            var data = entityPositions.MapItemFieldsByMapToAnnotation<GetInvCtrlPeriodViewModel>();

            // response wrapper
            return data;
        }
    }
}