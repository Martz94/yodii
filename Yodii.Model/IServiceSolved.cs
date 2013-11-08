﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Model
{
    public interface IServiceSolved
    {
        IServiceInfo ServiceInfo { get; }

        ServiceDisabledReason ConfigDisabledReason { get; }

        ConfigurationStatus ConfigurationStatus { get; }

        RunningRequirement ConfigSolvedStatus { get; }

        bool IsDisabled { get; }

        bool IsBlocking { get; }

        RunningStatus? RunningStatus { get; }
    }
}