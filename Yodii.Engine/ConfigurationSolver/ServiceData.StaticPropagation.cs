#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Engine\ConfigurationSolver\ServiceData.StaticPropagation.cs) is part of CiviKey. 
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
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;
using Yodii.Model;
using Yodii.Engine;
using CK.Core;

namespace Yodii.Engine
{
    partial class ServiceData
    {
        internal class StaticPropagation : BasePropagation
        {
            public StaticPropagation( ServiceData s )
                : base( s )
            {
            }

            public override void Refresh()
            {
                Refresh( Service.TotalAvailablePluginCount, Service.AvailablePluginCount, Service.AvailableServiceCount );
            }

            protected override bool IsValidPlugin( PluginData p )
            {
                return !p.Disabled;
            }

            protected override bool IsValidSpecialization( ServiceData s )
            {
                return !s.Disabled;
            }

            protected override BasePropagation GetPropagationInfo( ServiceData s )
            {
                return s.GetUsefulPropagationInfo();
            }

            public bool PropagateSolvedStatus()
            {
                Debug.Assert( Service.FinalConfigSolvedStatus == SolvedConfigurationStatus.Running );
                if( TheOnlyPlugin != null )
                {
                    if( !TheOnlyPlugin.SetRunningStatus( PluginRunningRequirementReason.FromServiceToSinglePlugin ) )
                    {
                        if( !Service.Disabled ) Service.SetDisabled( ServiceDisabledReason.PropagationToSinglePluginFailed );
                        else Service._configDisabledReason = ServiceDisabledReason.PropagationToSinglePluginFailed;
                        return false;
                    }
                }
                else if( TheOnlyService != null )
                {
                    if( !TheOnlyService.SetRunningStatus( ServiceSolvedConfigStatusReason.FromServiceToSingleSpecialization ) )
                    {
                        if( !Service.Disabled ) Service.SetDisabled( ServiceDisabledReason.PropagationToSingleSpecializationFailed );
                        else Service._configDisabledReason = ServiceDisabledReason.PropagationToSingleSpecializationFailed;
                        return false;
                    }
                }
                else
                {
                    StartDependencyImpact impact = Service.ConfigSolvedImpact | StartDependencyImpact.IsStartRunnableRecommended | StartDependencyImpact.IsStartRunnableOnly;

                    foreach( var s in GetIncludedServices( impact ) )
                    {
                        if( !s.SetRunningStatus( ServiceSolvedConfigStatusReason.FromPropagation ) )
                        {
                            if( !Service.Disabled ) Service.SetDisabled( ServiceDisabledReason.PropagationToCommonPluginReferencesFailed );
                            else Service._configDisabledReason = ServiceDisabledReason.PropagationToCommonPluginReferencesFailed;
                            return false;
                        }
                    }
                    //Debug.Assert( Service.Disabled || GetExcludedServices( impact ).All( s => s.Disabled ) );
                    if( !Service.Disabled )
                    {
                        foreach( var s in GetExcludedServices( impact ) )
                        {
                            if( !s.Disabled ) s.SetDisabled( ServiceDisabledReason.StopppedByPropagation );
                        }
                    }
                }
                return true;
            }

        }

        StaticPropagation _propagation;

        public bool PropagationIsUseless
        {
            get 
            { 
                return Disabled
                        || Family.Solver.Step < ConfigurationSolverStep.OnAllPluginsAdded 
                        || Family.RunningPlugin != null; 
            }
        }

        public bool PropagateSolvedStatus()
        {
            if( ConfigSolvedStatus == SolvedConfigurationStatus.Running )
            {
                var p = GetUsefulPropagationInfo();
                if( p != null ) return p.PropagateSolvedStatus();
            }
            return true;
        }

        StaticPropagation GetUsefulPropagationInfo()
        {
            if( Family.Solver.Step == ConfigurationSolverStep.InitializeFinalStartableStatus )
            {
                if( Disabled ) return null;
            }
            else if( PropagationIsUseless ) return null;
            if( _propagation == null ) _propagation = new StaticPropagation( this );
            _propagation.Refresh();
            return _propagation;
        }

        public void FillTransitiveIncludedServices( HashSet<ServiceData> set )
        {
            if( !set.Add( this ) ) return;
            var propagation = GetUsefulPropagationInfo();
            if( propagation == null ) return;
            foreach( var s in propagation.GetIncludedServices( ConfigSolvedImpact ) )
            {
                s.FillTransitiveIncludedServices( set );
            }
        }

    }
}
