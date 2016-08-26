#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Host\Plugin\CancelPreStopContext.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using Yodii.Model;

namespace Weavers.Plugin
{
    internal class CancelPresStopContext : IStartContext
    {
        readonly Dictionary<object, object> _shared;

        public CancelPresStopContext(Dictionary<object, object> shared)
        {
            _shared = shared;
        }

        public RunningStatus RunningStatus => RunningStatus.Disabled;

        public bool CancellingPreStop => true;

        public IDictionary<object, object> SharedMemory => _shared;

        public bool HotSwapping => false;

        public void PostAction(Action<IYodiiEngineExternal> delayedAction)
        {
        }
    }
}
