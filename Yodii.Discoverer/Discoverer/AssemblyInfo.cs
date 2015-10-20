#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Discoverer\Discoverer\AssemblyInfo.cs) is part of CiviKey. 
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using CK.Core;
using Yodii.Model;

namespace Yodii.Discoverer
{
    [Serializable]
    internal sealed class AssemblyInfo : IAssemblyInfo
    {
        readonly Uri _location;
        readonly AssemblyName _assemblyName;
        string _errorMessage;
        IReadOnlyList<ServiceInfo> _services;
        IReadOnlyList<PluginInfo> _plugins;
        IReadOnlyList<IDiscoveredItem> _items;

        internal AssemblyInfo( Uri location, string errorMessage )
        {
            Debug.Assert( location != null && errorMessage != null );
            _location = location;
            _errorMessage = errorMessage;
        }

        internal AssemblyInfo( string assemblyFullName, Uri location )
        {
            Debug.Assert( location != null );
            _location = location;
            _assemblyName = new AssemblyName( assemblyFullName );
        }

        public Uri AssemblyLocation
        {
            get { return _location; }
        }

        public AssemblyName AssemblyName { get { return _assemblyName; } }

        public IReadOnlyList<IServiceInfo> Services { get { return _services; } }

        public IReadOnlyList<IPluginInfo> Plugins { get { return _plugins; } }
        public IReadOnlyList<IDiscoveredItem> Items { get { return _items; } }

        internal void SetResult( IReadOnlyList<ServiceInfo> services, IReadOnlyList<PluginInfo> plugins )
        {
            Debug.Assert( services != null && plugins != null );
            _services = services;
            _plugins = plugins;
            _items = _services.Union<IDiscoveredItem>(_plugins).ToReadOnlyList();
        }

        internal void SetError( string message )
        {
            Debug.Assert( message != null );
            _errorMessage = message;
        }
    }
}
