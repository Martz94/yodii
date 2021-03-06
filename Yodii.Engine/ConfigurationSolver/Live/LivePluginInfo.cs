#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Engine\ConfigurationSolver\Live\LivePluginInfo.cs) is part of CiviKey. 
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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Yodii.Model;

namespace Yodii.Engine
{
    class LivePluginInfo : LiveYodiiItemInfo, ILivePluginInfo, INotifyRaisePropertyChanged
    {
        IPluginInfo _pluginInfo;
        ILiveServiceInfo _service;
        IPluginHostApplyCancellationInfo _currentError;

        internal LivePluginInfo( PluginData p, YodiiEngine engine )
            : base( engine, p, p.PluginInfo.PluginFullName )
        {
            _pluginInfo = p.PluginInfo;
        }

        public override bool IsPlugin { get { return true; } }

        internal void UpdateFrom( PluginData p, DelayedPropertyNotification notifier )
        {
            Debug.Assert( FullName == p.PluginInfo.PluginFullName );
            UpdateItem( p, notifier );
            notifier.Update( this, ref _pluginInfo, p.PluginInfo, () => PluginInfo );
        }

        internal void Bind( PluginData p, Func<string, LiveServiceInfo> serviceFinder, DelayedPropertyNotification notifier )
        {
            Debug.Assert( _pluginInfo == p.PluginInfo );
            var newService = p.Service != null ? serviceFinder( p.Service.ServiceInfo.ServiceFullName ) : null;
            notifier.Update( this, ref _service, newService, () => Service ); 
        }

        public ILiveServiceInfo Service
        {
            get { return _service; }
        }

        public IPluginInfo PluginInfo
        {
            get { return _pluginInfo; }
        }

        public IPluginHostApplyCancellationInfo CurrentError
        {
            get { return _currentError; }
            internal set
            {
                if( _currentError != value )
                {
                    _currentError = value;
                    RaisePropertyChanged();
                }
            }
        }

    }
}
