﻿using AutoMapper;
using AutoMapper.Configuration.Conventions;
using bbxBE.Application.BLL;
using bbxBE.Application.Commands.cmdImport;
using bbxBE.Common.Consts;
using bbxBE.Common.Exceptions;
using bbxBE.Application.Interfaces.Repositories;
using bbxBE.Application.Wrappers;
using bbxBE.Common;
using bbxBE.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Telerik.Reporting;
using Telerik.Reporting.Processing;
using Telerik.Reporting.XmlSerialization;

namespace bbxBE.Application.Commands.cmdInvCtrl
{
    public class PrintInvCtrlCommand : IRequest<FileStreamResult>
    {
        public long ID { get; set; }
        public string Title { get; set; }
        public bool IsInStock { get; set; }
        public string baseURL;
    }

    public class PrintInvCtrlCommandHandler : IRequestHandler<PrintInvCtrlCommand, FileStreamResult>
    {
        private readonly IInvCtrlRepositoryAsync _invCtrlRepository;
        private readonly IMapper _mapper;

        public PrintInvCtrlCommandHandler(IInvCtrlRepositoryAsync invCtrlRepository, IMapper mapper)
        {
            _invCtrlRepository = invCtrlRepository;
            _mapper = mapper;
        }

        public async Task<FileStreamResult> Handle(PrintInvCtrlCommand request, CancellationToken cancellationToken)
        {
            var reportTRDX = Utils.LoadEmbeddedResource("bbxBE.Application.Reports.InvCtrlICP.trdx", Assembly.GetExecutingAssembly());
            var res = await bllInvCtrl.CreateInvCtrlReportAsynch(_invCtrlRepository, reportTRDX, request, cancellationToken);

            return res;
        }


    }
}