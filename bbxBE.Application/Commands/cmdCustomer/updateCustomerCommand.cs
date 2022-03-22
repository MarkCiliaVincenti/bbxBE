﻿using AutoMapper;
using AutoMapper.Configuration.Conventions;
using bbxBE.Application.BLL;
using bbxBE.Application.Consts;
using bbxBE.Application.Interfaces.Repositories;
using bbxBE.Application.Wrappers;
using bbxBE.Common.NAV;
using bbxBE.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace bxBE.Application.Commands.cmdCustomer
{
    public class UpdateCustomerCommand : IRequest<Response<Customer>>
    {
        public long ID { get; set; }
        public string CustomerName { get; set; }

        public string CustomerBankAccountNumber { get; set; }

        public bool PrivatePerson { get; set; } = false;

        public string TaxpayerNumber { get; set; }          //9999999-9-99

        public string ThirdStateTaxId { get; set; }
        public string CountryCode { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string AdditionalAddressDetail { get; set; }
        public string Comment { get; set; }


    }

    public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, Response<Customer>>
    {
        private readonly ICustomerRepositoryAsync _customerRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public UpdateCustomerCommandHandler(ICustomerRepositoryAsync customerRepository, IMapper mapper, IConfiguration configuration)
        {
            _customerRepository = customerRepository;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<Response<Customer>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
            var cust = _mapper.Map<Customer>(request);

            if (request.TaxpayerNumber != null && !string.IsNullOrWhiteSpace(request.TaxpayerNumber.Replace("-", "")))
            {
                var TaxItems = request.TaxpayerNumber.Split('-');
                cust.TaxpayerId = TaxItems[0];
                cust.VatCode = TaxItems.Length > 1 ? TaxItems[1] : "";
                cust.CountyCode = TaxItems.Length > 2 ? TaxItems[2] : "";
            }
            cust.CustomerVatStatus = request.PrivatePerson ? CustomerVatStatusType.PRIVATE_PERSON.ToString() :
                                    string.IsNullOrWhiteSpace(request.CountryCode) || request.CountryCode == bbxBEConsts.CNTRY_HU ?
                                            CustomerVatStatusType.DOMESTIC.ToString()
                                            : CustomerVatStatusType.OTHER.ToString();

            cust.CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? bbxBEConsts.CNTRY_HU : cust.CountryCode.ToUpper();

            await _customerRepository.UpdateAsync(cust);
            return new Response<Customer>(cust);
        }


    }
}
