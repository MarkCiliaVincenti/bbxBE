﻿using bbxBE.Common.Consts;
using bbxBE.Application.Interfaces.Repositories;
using bbxBE.Application.Wrappers;
using bbxBE.Common.Enums;
using bxBE.Application.Commands.cmdInvCtrlPeriod;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace bbxBE.Application.Commands.cmdInvCtrlPeriod
{

    public class CloseInvCtrlPeriodCommandValidator : AbstractValidator<CloseInvCtrlPeriodCommand>
    {
        private readonly IInvCtrlPeriodRepositoryAsync _InvCtrlPeriodRepository;

        public CloseInvCtrlPeriodCommandValidator(IInvCtrlPeriodRepositoryAsync InvCtrlPeriodRepository)
        {
            this._InvCtrlPeriodRepository = InvCtrlPeriodRepository;

            RuleFor(r => r.ID)
             .NotEmpty().WithMessage(bbxBEConsts.ERR_REQUIRED)
                  .MustAsync(
                        async (model, Name, cancellation) =>
                        {
                            return await CanCloseAsync(model.ID, cancellation);
                        }
                    ).WithMessage(bbxBEConsts.ERR_INVCTRLPERIOD_ALREADYCLOSED);

        }


         private async Task<bool> CanCloseAsync( long ID, CancellationToken cancellationToken)
        {
            var canClose = await _InvCtrlPeriodRepository.CanCloseAsync(ID);
            return canClose;
        }

    }
}