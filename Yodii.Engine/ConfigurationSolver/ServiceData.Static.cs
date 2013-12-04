﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Yodii.Model;

namespace Yodii.Engine
{   
    internal partial class ServiceData
    {
        readonly Dictionary<string,ServiceData> _allServices;
        ServiceDisabledReason _configDisabledReason;
        SolvedConfigurationStatus _configSolvedStatus;
        ServiceSolvedConfigStatusReason _configSolvedStatusReason;
        ServiceData _configRunningSpecialization;
        ServiceData _configDirectRunningSpecialization;

        class BackReference
        {
            public readonly PluginData PluginData;
            public readonly DependencyRequirement Requirement;
            public readonly BackReference Next;

            public BackReference( BackReference next, PluginData p, DependencyRequirement req )
            {
                PluginData = p;
                Requirement = req;
                Next = next;
            }

        }
        BackReference _firstBackRunnableReference;

        public readonly IServiceInfo ServiceInfo;

        /// <summary>
        /// The direct generalization if any.
        /// When null, this instance is a <see cref="ServiceRootData"/>.
        /// </summary>
        public readonly ServiceData Generalization;

        /// <summary>
        /// Root of Generalization. Never null since when this is not a specialization, this is its own root.
        /// </summary>
        public readonly ServiceRootData GeneralizationRoot;

        /// <summary>
        /// The SolvedConfigStatus of the Service. 
        /// </summary>
        public readonly ConfigurationStatus ConfigOriginalStatus;

        internal ServiceData( Dictionary<string, ServiceData> allServices, IServiceInfo s, ServiceData generalization, ConfigurationStatus serviceStatus, Func<IServiceInfo,bool> isExternalServiceAvailable )
        {
            _allServices = allServices;
            ServiceInfo = s;
            if( (Generalization = generalization) != null )
            {
                GeneralizationRoot = Generalization.GeneralizationRoot;
                NextSpecialization = Generalization.FirstSpecialization;
                Generalization.FirstSpecialization = this;
                ++Generalization.SpecializationCount;
            }
            else
            {
                GeneralizationRoot = (ServiceRootData)this;
            }
            if ( (ConfigOriginalStatus = serviceStatus) == ConfigurationStatus.Disabled )
            {
                _configDisabledReason = ServiceDisabledReason.Config;
            }
            else if( s.HasError )
            {
                _configDisabledReason = ServiceDisabledReason.ServiceInfoHasError;
            }
            else if( Generalization != null && Generalization.Disabled )
            {
                _configDisabledReason = ServiceDisabledReason.GeneralizationIsDisabledByConfig;
            }
            //else if( !s.IsDynamicService && !isExternalServiceAvailable( s ) )
            //{
            //    _disabledReason = ServiceDisabledReason.ExternalServiceUnavailable;
            //}            
            _configSolvedStatusReason = ServiceSolvedConfigStatusReason.Config;
            if ( !Disabled )
            {
                Debug.Assert( (int)SolvedConfigurationStatus.Disabled == (int)ConfigurationStatus.Disabled );
                Debug.Assert( (int)SolvedConfigurationStatus.Optional == (int)ConfigurationStatus.Optional );
                Debug.Assert( (int)SolvedConfigurationStatus.Runnable == (int)ConfigurationStatus.Runnable );
                Debug.Assert( (int)SolvedConfigurationStatus.Running == (int)ConfigurationStatus.Running );
                _configSolvedStatus = (SolvedConfigurationStatus)serviceStatus;
            }
        }

        /// <summary>
        /// Gets whether this service is disabled. 
        /// </summary>
        public bool Disabled
        {
            get { return _configDisabledReason != ServiceDisabledReason.None; }
        }

        public ServiceData MustExistSpecialization
        {
            get { return _configRunningSpecialization; }
        }

        private bool IsStrictGeneralizationOf( ServiceData d )
        {
            var g = d.Generalization;
            if( g == null || d.GeneralizationRoot != GeneralizationRoot ) return false;
            do
            {
                if( g == this ) return true;
                g = g.Generalization;
            }
            while( g != null );
            return false;
        }

        /// <summary>
        /// Gets the first reason why this service is disabled. 
        /// </summary>
        public ServiceDisabledReason DisabledReason
        {
            get { return _configDisabledReason; }
        }

        internal virtual void SetDisabled( ServiceDisabledReason r )
        {
            Debug.Assert( r != ServiceDisabledReason.None );
            Debug.Assert( _configDisabledReason == ServiceDisabledReason.None );
            Debug.Assert( !GeneralizationRoot.Disabled ||
                (GeneralizationRoot.DisabledReason == ServiceDisabledReason.MultipleSpecializationsRunningByConfig
                || GeneralizationRoot.DisabledReason == ServiceDisabledReason.GeneralizationIsDisabled)
                && r == ServiceDisabledReason.GeneralizationIsDisabled, "A root is necessarily not disabled if one of its specialization is not disabled except if we are propagating a MultipleSpecializationsRunningByConfig to the specialized Services." );
            _configDisabledReason = r;
            ServiceData spec = FirstSpecialization;
            while( spec != null )
            {
                if( !spec.Disabled ) spec.SetDisabled( ServiceDisabledReason.GeneralizationIsDisabled );
                spec = spec.NextSpecialization;
            }
            PluginData plugin = FirstPlugin;
            while( plugin != null )
            {
                if( !plugin.Disabled ) plugin.SetDisabled( PluginDisabledReason.ServiceIsDisabled );
                plugin = plugin.NextPluginForService;
            }
            Debug.Assert( _theOnlyPlugin == null && _commonReferences == null, "Disabling all plugins must have set them to null." );
            // The _mustExistReferencer list contains plugins that has at least a MustExist reference to this service
            // and have been initialized when this Service was not yet disabled.
            BackReference br = _firstBackRunnableReference;
            while( br != null )
            {
                if( !br.PluginData.Disabled ) br.PluginData.SetDisabled( PluginDisabledReason.MustExistReferenceIsDisabled );
                br = br.Next;
            }
            _configDirectRunningSpecialization = null;
            _configRunningSpecialization = null;
        }


        /// <summary>
        /// Gets the minimal running requirement. It is initialized by the configuration, but may evolve.
        /// </summary>
        public SolvedConfigurationStatus ConfigSolvedStatus
        {
            get { return _configRunningSpecialization != null ? _configRunningSpecialization._configSolvedStatus : _configSolvedStatus; }
        }

        /// <summary>
        /// Gets the minimal running requirement for this service (not the one of MustExistSpecialization if it exists).
        /// </summary>
        public SolvedConfigurationStatus ThisMinimalRunningRequirement
        {
            get { return _configSolvedStatus; }
        }

        /// <summary>
        /// Gets the strongest reason that explains this service ThisMinimalRunningRequirement. 
        /// </summary>
        public ServiceSolvedConfigStatusReason ThisRunningRequirementReason 
        {
            get { return _configSolvedStatusReason; }
        }

        /// <summary>
        /// This can be called on an already disabled service and may trigger changes on the whole system.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="reason"></param>
        /// <returns>True if the requirement can be satisfied at this level. False otherwise.</returns>
        internal bool SetSolvedConfigurationStatus( SolvedConfigurationStatus s, ServiceSolvedConfigStatusReason reason )
        {
            if( _configRunningSpecialization != null && _configRunningSpecialization != this )
            {
                Debug.Assert( _configRunningSpecialization._configSolvedStatus == SolvedConfigurationStatus.Running );
                return _configRunningSpecialization.SetSolvedConfigurationStatus( s, reason );
            }
            if( s <= _configSolvedStatus )
            {
                if( s >= SolvedConfigurationStatus.Runnable ) return !Disabled;
                return true;
            }
            // New requirement is stronger than the previous one.
            // We now try to honor the requirement at the service level.
            // If we fail, this service will be disabled, but we set the requirement to prevent reentrancy.
            // Reentrancy can nevertheless be caused by subsequent requirements MustExistTryStart or MustExistAndRun:
            // we allow this (there will be at most 3 reentrant calls to this method). 
            // Note that we capture the reason only on the first call, not on each failing call: the reason is not necessarily 
            // associated to the running requirement.
            var current = _configSolvedStatus;
            _configSolvedStatus = s;
            // Is it compliant with a Disabled service? If yes, it is always satisfied.
            if( s < SolvedConfigurationStatus.Runnable )
            {
                _configSolvedStatusReason = reason;
                // Propagate TryStart.
                PropagateRunningRequirementToOnlyPluginOrCommonReferences();
                return true;
            }
            // The new requirement is at least Runnable.
            // If this is already disabled, there is nothing to do.
            if( Disabled ) return false;

            _configSolvedStatusReason = reason;

            // Call SetAsRunningService only if this Service becomes Runnable or Running.
            if( _configSolvedStatus == SolvedConfigurationStatus.Running && current < SolvedConfigurationStatus.Running )
            {
                if( !SetAsRunningService() ) return false;
            }
            Debug.Assert( !Disabled );
            // Now, if the OnlyPlugin exists, propagate the MustExist (or more) requirement to it.
            // MustExist requirement triggers MustExist on MustExist plugins to services requirements.
            // (This can be easily propagated if there is one and only one plugin for the service.)
            //
            // If more than one plugin exist, we can actually propagate requirements to all the Services that are shared 
            // by all our non-disabled plugins: we initialize our CommonServiceReferences object.
            //
            if( TotalAvailablePluginCount > 1 ) InitializePropagation( TotalAvailablePluginCount, fromConfig:false );
            PropagateRunningRequirementToOnlyPluginOrCommonReferences();
            return !Disabled;
        }

        /// <summary>
        /// Called by SetSolvedConfigurationStatus whenever the Requirement becomes Running, or by ServiceRootData.OnAllPluginsAdded
        /// if a RunningPluginByConfig exists for the root.
        /// </summary>
        /// <returns></returns>
        internal bool SetAsRunningService( bool fromRunningPlugin = false )
        {
            if( fromRunningPlugin )
            {
                Debug.Assert( GeneralizationRoot.RunningPluginByConfig.ConfigSolvedStatus == SolvedConfigurationStatus.Running );
                _configSolvedStatus = SolvedConfigurationStatus.Running;
            }
            Debug.Assert( _configSolvedStatus == SolvedConfigurationStatus.Running );
            // From a non running requirement to a running requirement.
            var currentRunning = _configRunningSpecialization;
            
            //
            // Only 3 possible cases here:
            //
            // - There is no current Runnable Service for our Generalization.
            // - we are the current one... We are necessarily already be Runnable.
            // - We specialize the current one.
            //
            Debug.Assert( currentRunning == null || currentRunning.IsStrictGeneralizationOf( this ) );
            // Note: The other cases would be:
            //    - We are a Generalization of the current one. This is not possible since SetRunningRequirement is routed to the _mustExistSpecialization if it exists.
            //    - a specialization exists and we are not a specialization nor a generalization of it: this is not possible since we would have been disabled.
            //

            // We must disable all sibling services (and plugins) from this up to Runnable (when Runnable is null, up to the root).
            _configRunningSpecialization = this;
            var g = Generalization;
            if( g != null )
            {
                var specThatMustExist = this;
                do
                {
                    g._configRunningSpecialization = this;
                    g._configDirectRunningSpecialization = specThatMustExist;
                    specThatMustExist = g;
                    g = g.Generalization;
                }
                while( g != null );
            }

            g = Generalization;
            if( g != null )
            {
                var specThatMustExist = this;
                do
                {
                    Debug.Assert( g._configRunningSpecialization == this );
                    Debug.Assert( g._configDirectRunningSpecialization == specThatMustExist );
                    var spec = g.FirstSpecialization;
                    while( spec != null )
                    {
                        if( spec != specThatMustExist && !spec.Disabled ) spec.SetDisabled( ServiceDisabledReason.AnotherSpecializationMustRun );
                        spec = spec.NextSpecialization;
                    }
                    PluginData p = g.FirstPlugin;
                    while( p != null )
                    {
                        if( !p.Disabled ) p.SetDisabled( PluginDisabledReason.ServiceSpecializationMustRun );
                        p = p.NextPluginForService;
                    }
                    specThatMustExist = g;
                    g = g.Generalization;
                }
                while( g != null );
            }
            if( Disabled ) return false;
            return true;
        }

        /// <summary>
        /// Head of the linked list of ServiceData that specialize Service.
        /// </summary>
        public ServiceData FirstSpecialization;

        /// <summary>
        /// Linked list to another ServiceData that specialize Service.
        /// </summary>
        public readonly ServiceData NextSpecialization;

        /// <summary>
        /// Number of direct specializations.
        /// </summary>
        public int SpecializationCount;

        /// <summary>
        /// Head of the linked list of PluginData that implement this exact Service (not specialized ones).
        /// </summary>
        public PluginData FirstPlugin;

        /// <summary>
        /// Number of plugins for this exact service.
        /// </summary>
        public int PluginCount;

        /// <summary>
        /// Number of plugins for this exact service that are disabled.
        /// </summary>
        public int DisabledPluginCount;

        /// <summary>
        /// Gets the number of available plugins for this exact service ((<see cref="PluginCount"/> - <see cref="DisabledPluginCount"/>).
        /// </summary>
        public int AvailablePluginCount
        {
            get { return PluginCount - DisabledPluginCount; }
        }

        /// <summary>
        /// Number of total plugins (the ones for this service and for any of our specializations).
        /// </summary>
        public int TotalPluginCount;

        /// <summary>
        /// Number of total plugins that are disabled (the ones for this service and for any of our specializations).
        /// </summary>
        public int TotalDisabledPluginCount;

        /// <summary>
        /// Gets the number of available plugins (<see cref="TotalPluginCount"/> - <see cref="TotalDisabledPluginCount"/>)
        /// for this service and its specializations.
        /// </summary>
        public int TotalAvailablePluginCount
        {
            get { return TotalPluginCount - TotalDisabledPluginCount; }
        }

        /// <summary>
        /// First step after object construction.
        /// </summary>
        /// <returns>The deepest specialization that must run or null if no running service or a conflict exists.</returns>
        protected ServiceData GetRunningService()
        {
            Debug.Assert( !Disabled, "Must NOT be called on already disabled service." );
            // Handles direct specializations that MustExist.
            ServiceData directSpecThatMustRun = null;
            ServiceData specRunning = null;
            ServiceData spec = FirstSpecialization;
            while( spec != null )
            {
                if( !spec.Disabled )
                {
                    var running = spec.GetRunningService();
                    // We may stop as soon as a conflict is detected, but we continue the loop to let any branches detect other potential conflicts.
                    if( !Disabled )
                    {
                        if( spec.DisabledReason == ServiceDisabledReason.MultipleSpecializationsRunningByConfig )
                        {
                            Debug.Assert( running == null, "Since a conflict has been detected below, returned mustExist is null." );
                            SetDisabled( ServiceDisabledReason.MultipleSpecializationsRunningByConfig );
                            directSpecThatMustRun = specRunning = null;
                        }
                        else
                        {
                            Debug.Assert( spec.Disabled == false, "Since it was not disabled before calling GetRunningService, it could only be ServiceDisabledReason.MultipleSpecializationsMustExist." );
                            if( running != null )
                            {
                                if( specRunning != null )
                                {
                                    SetDisabled( ServiceDisabledReason.MultipleSpecializationsRunningByConfig );
                                    directSpecThatMustRun = specRunning = null;
                                }
                                else
                                {
                                    specRunning = running;
                                    directSpecThatMustRun = spec;
                                }
                            }
                        }
                    }
                }
                spec = spec.NextSpecialization;
            }
            Debug.Assert( !Disabled || specRunning == null, "(Conflict below <==> Disabled) => specMustExist has been set to null." );
            Debug.Assert( (specRunning != null) == (directSpecThatMustRun != null) );
            if( !Disabled )
            {
                // No specialization is required to exist, is it our case?
                if( specRunning == null )
                {
                    Debug.Assert( ConfigOriginalStatus != ConfigurationStatus.Disabled, "Caution: Disabled is greater than MustExist." );
                    if( ConfigOriginalStatus == ConfigurationStatus.Running ) specRunning = _configRunningSpecialization = this;
                }
                else
                {
                    // A specialization must exist: it must be the only one, others are disabled.
                    spec = FirstSpecialization;
                    while( spec != null )
                    {
                        if( spec != directSpecThatMustRun && !spec.Disabled )
                        {
                            spec.SetDisabled( ServiceDisabledReason.AnotherSpecializationMustExistByConfig );
                        }
                        spec = spec.NextSpecialization;
                    }
                    _configRunningSpecialization = specRunning;
                    _configDirectRunningSpecialization = directSpecThatMustRun;
                }
                Debug.Assert( !Disabled, "The fact that this service (or a specialization) must exist, can not disable this service." );
            }
            return specRunning;
        }


        PluginData FindFirstPluginData( Func<PluginData, bool> filter )
        {
            PluginData p = FirstPlugin;
            while( p != null )
            {
                if( filter( p ) ) return p;
                p = p.NextPluginForService;
            }
            ServiceData s = FirstSpecialization;
            while( s != null )
            {
                p = s.FindFirstPluginData( filter );
                if( p != null ) return p;
                s = s.NextSpecialization;
            }
            return null;
        }

        internal void AddPlugin( PluginData p )
        {
            // Consider its RunningRequirements to detect trivial case: the fact that another plugin 
            // must run for the same Generalization service.
            // The less trivially case when this running plugin conflicts with some other Running at the services level
            // is already handled in PluginData constructor thanks to service.MustExistSpecialization being not null that 
            // immediately disables the plugin.
            if( p.ConfigSolvedStatus == SolvedConfigurationStatus.Running )
            {
                Debug.Assert( !p.Disabled );
                GeneralizationRoot.SetRunningPluginByConfig( p );
            }
            // Adds the plugin, taking its disabled state into account.
            p.NextPluginForService = FirstPlugin;
            FirstPlugin = p;
            ++PluginCount;
            if( p.Disabled ) ++DisabledPluginCount;
            ServiceData g = this;
            do
            {
                ++g.TotalPluginCount;
                if( p.Disabled ) ++g.TotalDisabledPluginCount;
                g = g.Generalization;
            }
            while( g != null );
        }

        /// <summary>
        /// Adds a plugin that requires this service with Runnable, RunnableTryStart or Running requirement.
        /// </summary>
        /// <param name="plugin">The plugin that references us.</param>
        internal void AddRunnableReferencer( PluginData plugin, DependencyRequirement req )
        {
            Debug.Assert( !Disabled );
            Debug.Assert( req >= DependencyRequirement.Runnable );
            _firstBackRunnableReference = new BackReference( _firstBackRunnableReference, plugin, req );
        }

        internal virtual void OnAllPluginsAdded()
        {
            Debug.Assert( !Disabled, "Must NOT be called on already disabled service." );
            Debug.Assert( (MustExistSpecialization == null || MustExistSpecialization == this) || PluginCount == DisabledPluginCount, "If there is a must exist specialization, all our plugins are disabled." );

            // Recursive call: the only plugin or the CommonServiceReferences are
            // updated bottom up, so that this Generalization can reuse them.
            ServiceData spec = FirstSpecialization;
            while( spec != null )
            {
                if( !spec.Disabled ) spec.OnAllPluginsAdded();
                spec = spec.NextSpecialization;
            }
            // For raw Service (from Service container) we have nothing to do... 
            // they are available or not (and then they are Disabled).
            if( !Disabled )
            {
                if( TotalPluginCount == 0 )
                {
                    SetDisabled( ServiceDisabledReason.NoPlugin );
                }
                else
                {
                    int nbAvailable = TotalAvailablePluginCount;
                    if( nbAvailable == 0 )
                    {
                        SetDisabled( ServiceDisabledReason.AllPluginsAreDisabled );
                    }
                    else InitializePropagation( nbAvailable, fromConfig: true );
                }
            }
        }

        internal void OnPluginDisabled( PluginData p )
        {
            Debug.Assert( (p.Service == this || IsStrictGeneralizationOf( p.Service )) && p.Disabled );
            if( p.Service == this ) ++DisabledPluginCount;
            ++TotalDisabledPluginCount;
            if ( !Disabled )
            {
                int nbAvailable = TotalAvailablePluginCount;
                if ( nbAvailable == 0 )
                {
                    _theOnlyPlugin = null;
                    _commonReferences = null;
                    SetDisabled( ServiceDisabledReason.AllPluginsAreDisabled );
                }
                else InitializePropagation( nbAvailable, fromConfig: false );
            }
            // We must update and check the total number of plugins.
            if( Generalization != null ) Generalization.OnPluginDisabled( p );
        }

        public override string ToString()
        {
            return String.Format( "{0} - {1} - {2} - {4} plugins => ((Dynamic: {3})", ServiceInfo.ServiceFullName, Disabled ? DisabledReason.ToString() : "!Disabled", ConfigSolvedStatus, _dynamicStatus, TotalAvailablePluginCount );
        }
    }
}
