﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yodii.Model;

namespace Yodii.Host.Tests
{
    public interface IChoucrouteService4 : IYodiiService
    {
        void DoSomething();
        List<string> CalledMethods { get; }
    }
}