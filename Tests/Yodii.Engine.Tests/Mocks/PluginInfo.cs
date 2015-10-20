#region LGPL License
/*----------------------------------------------------------------------------
* This file (Tests\Yodii.Engine.Tests\Mocks\PluginInfo.cs) is part of CiviKey. 
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
using System.Text;
using System.Threading.Tasks;
using Yodii.Model;
using CK.Core;

namespace Yodii.Engine.Tests.Mocks
{
    public class PluginInfo : ItemInfo, IPluginInfo, IPluginCtorInfo
    {
        readonly List<IServiceReferenceInfo> _serviceReferences;
        IServiceInfo _service;
 
        internal PluginInfo( string pluginFullName, IAssemblyInfo assemblyInfo ): base(pluginFullName, assemblyInfo)
        {
            _serviceReferences = new List<IServiceReferenceInfo>();
        }

        internal void BindServiceRequirement( IServiceReferenceInfo reference )
        {
            Debug.Assert( !_serviceReferences.Contains( reference ) );
            Debug.Assert( reference.Owner == this );

            _serviceReferences.Add( reference );
        }

        public string PluginFullName
        {
            get { return FullName; }
        }

        public List<IServiceReferenceInfo> ServiceReferences
        {
            get { return _serviceReferences; }
        }

        public ServiceReferenceInfo AddServiceReference( ServiceInfo service, DependencyRequirement req )
        {
            var r = new ServiceReferenceInfo( this, service, req );
            _serviceReferences.Add( r );
            return r;
        }

        public IPluginCtorInfo ConstructorInfo
        {
            get { return this; }
        }

        int IPluginCtorInfo.ParameterCount
        {
            get { return _serviceReferences.Count; }
        }

        IReadOnlyList<IPluginCtorKnownParameterInfo> IPluginCtorInfo.KnownParameters
        {
            get { return CKReadOnlyListEmpty<IPluginCtorKnownParameterInfo>.Empty; }
        }

        public IServiceInfo Service
        {
            get { return _service; }
            set
            {
                if ( _service != null ) ((ServiceInfo)_service).RemovePlugin( this );
                _service = value;
                if ( _service != null ) ((ServiceInfo)_service).AddPlugin( this );
            }
        }

        IReadOnlyList<IServiceReferenceInfo> IPluginInfo.ServiceReferences
        {
            get { return _serviceReferences.AsReadOnlyList(); }
        }
    }
}
