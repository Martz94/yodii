#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Host\YodiiHost.cs) is part of CiviKey. 
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

using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Yodii.Model;
using System.Reflection;
using System.Text;

namespace Yodii.Host
{
    public class YodiiHost : IYodiiEngineHost
    {
        readonly IActivityMonitor _monitor;
        readonly ServiceHost _serviceHost;
        readonly Dictionary<string, PluginProxy> _plugins;
        Func<IPluginInfo,object[],IYodiiPlugin> _pluginCreator;

        public YodiiHost()
            : this( null, CatchExceptionGeneration.HonorIgnoreExceptionAttribute )
        {
        }

        public YodiiHost( IActivityMonitor monitor, CatchExceptionGeneration exceptionGeneration = CatchExceptionGeneration.HonorIgnoreExceptionAttribute )
        {
            if( monitor == null ) monitor = new ActivityMonitor( "Yodii.Host.YodiiHost");
            _monitor = monitor;
            _plugins = new Dictionary<string, PluginProxy>();
            _serviceHost = new ServiceHost( exceptionGeneration );
            _pluginCreator = DefaultPluginCreator;
        }

        IYodiiPlugin DefaultPluginCreator( IPluginInfo pluginInfo, object[] ctorParameters )
        {
            using( _monitor.OpenTrace().Send( "Using DefaultCreator for {0}.", pluginInfo.PluginFullName ) )
            {
                var tPlugin = Assembly.Load( pluginInfo.AssemblyInfo.AssemblyName ).GetType( pluginInfo.PluginFullName, true );
                var ctor = tPlugin.GetConstructors().OrderBy( c => c.GetParameters().Length ).Last();
                if( ctorParameters.Length != ctor.GetParameters().Length || ctorParameters.Any( p => p == null ) )
                {
                    throw new CKException( R.DefaultPluginCreatorUnresolvedParams );
                }
                return (IYodiiPlugin)ctor.Invoke( ctorParameters );
            }
        }

        /// <summary>
        /// Gets or sets a function that is in charge of obtaining concrete plugin instances.
        /// The dynamic services parameters is available in the order 
        /// of <see cref="IServiceReferenceInfo.ConstructorParameterIndex">ConstructorParameterIndex</see> property 
        /// of <see cref="IPluginInfo.ServiceReferences">PluginInfo.ServiceReferences</see> objects.
        /// </summary>
        public Func<IPluginInfo, object[], IYodiiPlugin> PluginCreator
        {
            get { return _pluginCreator; }
            set { _pluginCreator = value ?? DefaultPluginCreator; }
        }

        public IServiceHost ServiceHost
        {
            get { return _serviceHost; }
        }

        public ILogCenter LogCenter
        {
            get { return _serviceHost; }
        }

        /// <summary>
        /// Gets the <see cref="IPluginProxy"/> for the plugin identifier. 
        /// It may find plugins that are currently disabled but have been loaded at least once.
        /// </summary>
        /// <param name="pluginId">Plugin identifier.</param>
        /// <returns>Null if not found.</returns>
        public IPluginProxy FindLoadedPlugin( string pluginFullName )
        {
            return _plugins.GetValueWithDefault( pluginFullName, null );
        }

        class Result : IYodiiEngineHostApplyResult
        {
            public Result( IReadOnlyList<IPluginHostApplyCancellationInfo> errors )
            {
                CancellationInfo = errors;
            }

            public IReadOnlyList<IPluginHostApplyCancellationInfo> CancellationInfo { get; private set; }

        }

        /// <summary>
        /// Attempts to execute a plan.
        /// </summary>
        /// <param name="disabledPlugins">Plugins that must be disabled.</param>
        /// <param name="stoppedPlugins">Plugins that must be stopped.</param>
        /// <param name="runningPlugins">Plugins that must be running.</param>
        /// <returns>A <see cref="IYodiiEngineHostApplyResult"/> that details the error if any.</returns>
        public IYodiiEngineHostApplyResult Apply( 
            IEnumerable<IPluginInfo> disabledPlugins, 
            IEnumerable<IPluginInfo> stoppedPlugins, 
            IEnumerable<IPluginInfo> runningPlugins,
            Action<Action<IYodiiEngine>> postStartActionsCollector )
        {
            if( disabledPlugins == null ) throw new ArgumentNullException( "disabledPlugins" );
            if( stoppedPlugins == null ) throw new ArgumentNullException( "stoppedPlugins" );
            if( runningPlugins == null ) throw new ArgumentNullException( "runningPlugins" );
            if( PluginCreator == null ) throw new InvalidOperationException( R.PluginCreatorIsNull );

            using( _monitor.OpenInfo().Send( "Applying plan..." ) )
            {
                var s = _monitor.Info();
                if( !s.IsRejected )
                {
                    s.Send( new StringBuilder()
                                .Append( "DisabledPlugins: " )
                                .Append( disabledPlugins.Select( p => p.PluginFullName ) )
                                .AppendLine()
                                .Append( "StoppedPlugins: " )
                                .Append( stoppedPlugins.Select( p => p.PluginFullName ) )
                                .AppendLine()
                                .Append( "RunningPlugins: " )
                                .Append( runningPlugins.Select( p => p.PluginFullName ) )
                                .ToString() );
                }
                return DoApply( disabledPlugins, stoppedPlugins, runningPlugins, postStartActionsCollector );
            }
        }

        IYodiiEngineHostApplyResult DoApply( 
            IEnumerable<IPluginInfo> disabledPlugins, 
            IEnumerable<IPluginInfo> stoppedPlugins, 
            IEnumerable<IPluginInfo> runningPlugins,
            Action<Action<IYodiiEngine>> postStartActionsCollector )
        {
            HashSet<IPluginInfo> uniqueCheck = new HashSet<IPluginInfo>();
            foreach( var input in disabledPlugins.Concat( stoppedPlugins ).Concat( runningPlugins ) )
            {
                if( !uniqueCheck.Add( input ) )
                {
                    throw new ArgumentException( String.Format( R.HostApplyPluginMustBeInOneList, input.PluginFullName ) );
                }
            }

            var errors = new List<CancellationInfo>();

            ServiceManager serviceManager = new ServiceManager( _serviceHost );
            // The toStart and toStop list are lists of StStart/StStopContext.
            // With the help of the ServiceManager, this resolves the issue to find swapped plugins (and their most specialized common service).
            List<StStopContext> toStop = new List<StStopContext>();
            List<StStartContext> toStart = new List<StStartContext>();

            // To be able to initialize StContext objects, we need to instanciate the shared memory now.
            Dictionary<object, object> sharedMemory = new Dictionary<object, object>();

            using( _monitor.OpenTrace().Send( "Computing plugins to Stop from disabled ones: " ) )
            {
                #region Disabled Plugins
                foreach( IPluginInfo k in disabledPlugins )
                {
                    PluginProxy p = EnsureProxy( k );
                    Debug.Assert( p.Status != PluginStatus.Stopping && p.Status != PluginStatus.Starting );
                    if( p.Status != PluginStatus.Null )
                    {
                        var preStop = new StStopContext( p, sharedMemory, true, p.Status == PluginStatus.Stopped );
                        if( k.Service != null ) serviceManager.AddToStop( k.Service, preStop );
                        toStop.Add( preStop );
                    }
                }
                _monitor.CloseGroup( toStop.Select( c => c.Plugin.PluginInfo.PluginFullName ).Concatenate() );
                #endregion
            }
            using( _monitor.OpenTrace().Send( "Adding plugins to Stop from stopping plugins: " ) )
            {
                #region Stopped Plugins
                foreach( IPluginInfo k in stoppedPlugins )
                {
                    PluginProxy p = EnsureProxy( k );
                    Debug.Assert( p.Status != PluginStatus.Stopping && p.Status != PluginStatus.Starting );
                    if( p.Status == PluginStatus.Started )
                    {
                        var preStop = new StStopContext( p, sharedMemory, false, false );
                        if( k.Service != null ) serviceManager.AddToStop( k.Service, preStop );
                        toStop.Add( preStop );
                    }
                    _monitor.CloseGroup( toStop.Select( c => c.Plugin.PluginInfo.PluginFullName ).Concatenate() );
                }
                #endregion
            }

            // Now, we attempt to activate the plugins that must run: if an error occurs,
            // we leave and return the error since we did not change anything.
            using( _monitor.OpenTrace().Send( "Registering running plugins: " ) )
            {
                #region Running Plugins. Leave on first Load error.
                _serviceHost.CallServiceBlocker = calledServiceType => new ServiceCallBlockedException( calledServiceType, R.CallingServiceFromCtor );
                foreach( IPluginInfo k in runningPlugins )
                {
                    PluginProxy p = EnsureProxy( k );
                    if( p.Status != PluginStatus.Started )
                    {
                        if( p.RealPluginObject == null )
                        {
                            using( _monitor.OpenTrace().Send( "Instanciating '{0}'.", k.PluginFullName ) )
                            {
                                if( !p.TryLoad( _serviceHost, PluginCreator ) )
                                {
                                    Debug.Assert( p.LoadError != null, "Error is catched by the PluginHost itself." );
                                    _monitor.Error().Send( p.LoadError, "Instanciation failed" );
                                    _serviceHost.LogMethodError( PluginCreator.Method, p.LoadError );
                                    // Unable to load the plugin: leave now.
                                    errors.Add( new CancellationInfo( p.PluginInfo, true ) { Error = p.LoadError, ErrorMessage = R.ErrorWhileCreatingPluginInstance } );
                                    // Breaks the loop: stop on the first failure.
                                    // It is useless to pre load next plugins as long as we can be sure that they will not run now. 
                                    break;
                                }
                                Debug.Assert( p.Status == PluginStatus.Null );
                            }
                        }
                        var preStart = new StStartContext( p, sharedMemory, p.Status == PluginStatus.Null );
                        p.Status = PluginStatus.Stopped;
                        if( k.Service != null ) serviceManager.AddToStart( k.Service, preStart );
                        toStart.Add( preStart );
                    }
                }
                _serviceHost.CallServiceBlocker = null;
                if( errors.Count > 0 )
                {
                    // Restores Disabled states.
                    CancelSuccessfulStartForDisabled( toStart );
                    return new Result( errors.AsReadOnlyList() );
                }
                _monitor.CloseGroup( toStart.Select( c => c.Plugin.PluginInfo.PluginFullName ).Concatenate() );
                #endregion
            }

            // The toStart list of StStartContext is ready (and plugins inside are loaded without error).
            // Now starts the actual PreStop/PreStart/Stop/Start/Disable phase:

            using( _monitor.OpenTrace().Send( "Calling PreStop." ) )
            {
                #region Calling PreStop for all "toStop" plugins: calls to Services are allowed.
                foreach( var stopC in toStop )
                {
                    if( !stopC.IsDisabledOnly )
                    {
                        using( _monitor.OpenTrace().Send( "Plugin: {0}.", stopC.Plugin.PluginInfo.PluginFullName ) )
                        {
                            Debug.Assert( stopC.Plugin.Status == PluginStatus.Started );
                            try
                            {
                                stopC.Plugin.RealPluginObject.PreStop( stopC );
                            }
                            catch( Exception ex )
                            {
                                _monitor.Error().Send( ex );
                                stopC.Cancel( ex.Message, ex );
                                _serviceHost.LogMethodError( stopC.Plugin.GetImplMethodInfoPreStop(), ex );
                            }
                            if( stopC.HandleSuccess( errors, false ) )
                            {
                                stopC.Plugin.Status = PluginStatus.Stopping;
                            }
                        }
                    }
                }
                #endregion
            }
            // If at least one failed, cancel the start for the successful ones
            // and stops here.
            if( errors.Count > 0 )
            {
                CancelSuccessfulPreStop( sharedMemory, toStop );
                // Restores Disabled states.
                CancelSuccessfulStartForDisabled( toStart );
                return new Result( errors.AsReadOnlyList() );
            }

            // PreStop has been successfully called: we must now call the PreStart.
            // Calls to Services are not allowed during PreStart.
            using( _monitor.OpenTrace().Send( "Calling PreStart." ) )
            {
                #region Calling PreStart.
                _serviceHost.CallServiceBlocker = calledServiceType => new ServiceCallBlockedException( calledServiceType, R.CallingServiceFromPreStart );
                foreach( var startC in toStart )
                {
                    Debug.Assert( startC.Plugin.Status == PluginStatus.Stopped );
                    try
                    {
                        startC.Plugin.RealPluginObject.PreStart( startC );
                    }
                    catch( Exception ex )
                    {
                        startC.Cancel( ex.Message, ex );
                    }
                    if( startC.HandleSuccess( errors, true ) )
                    {
                        startC.Plugin.Status = PluginStatus.Starting;
                    }
                }
                _serviceHost.CallServiceBlocker = null;
                #endregion
            }
            // If a PreStart failed, we cancel everything and stop.
            if( errors.Count > 0 )
            {
                CancelSuccessfulPreStop( sharedMemory, toStop );
                _serviceHost.CallServiceBlocker = calledServiceType => new ServiceCallBlockedException( calledServiceType, R.CallingServiceFromPreStartRollbackAction );
                var cStop = new CancelPreStartContext( sharedMemory );
                foreach( var c in toStart )
                {
                    if( c.Success )
                    {
                        c.Plugin.Status = PluginStatus.Stopped;
                        var rev = c.RollbackAction;
                        if( rev != null ) rev( cStop );
                        else c.Plugin.RealPluginObject.Stop( cStop );
                    }
                }
                _serviceHost.CallServiceBlocker = null;
                // Restores Disabled states.
                CancelSuccessfulStartForDisabled( toStart );
                return new Result( errors.AsReadOnlyList() );
            }

            // Setting ServiceStatus & sending events: StoppingSwapped, Stopping, StartingSwapped, Starting. 
            SetServiceSatus( postStartActionsCollector, toStop, toStart, true );


            // Time to call Stop. While Stopping, calling Services is not allowed.
            string callingPluginName = null;
            _serviceHost.CallServiceBlocker = calledServiceType => new ServiceCallBlockedException( calledServiceType, String.Format( R.CallingServiceFromStop, callingPluginName ) );
            // Even if we throw the exception here, we always clear the CallServiceBlocker on error.
            try
            {
                foreach( var stopC in toStop )
                {
                    if( !stopC.IsDisabledOnly )
                    {
                        Debug.Assert( stopC.Plugin.Status == PluginStatus.Stopping );
                        callingPluginName = stopC.Plugin.PluginInfo.PluginFullName;
                        stopC.Plugin.Status = PluginStatus.Stopped;
                        stopC.Plugin.RealPluginObject.Stop( stopC );
                    }
                }
            }
            finally
            {
                _serviceHost.CallServiceBlocker = null;
            }
            // Before calling Start, we must set the implementations on Services
            // to be the starting plugin.
            foreach( var startC in toStart )
            {
                Debug.Assert( startC.Plugin.Status == PluginStatus.Starting );
                var impact = startC.ServiceImpact;
                while( impact != null )
                {
                    Debug.Assert( impact.Service.Status == ServiceStatus.Starting || impact.Service.Status == ServiceStatus.StartingSwapped );
                    Debug.Assert( impact.Implementation.Plugin == startC.Plugin || impact.SwappedImplementation.Plugin == startC.Plugin );
                    impact.Service.SetPluginImplementation( startC.Plugin );
                    impact = impact.ServiceGeneralization;
                }
            }
            // Now that all services are bound, starts the plugin.
            foreach( var startC in toStart )
            {
                startC.Plugin.Status = PluginStatus.Started;
                startC.Plugin.RealPluginObject.Start( startC );
            }

            // Setting services status & sending events...
            SetServiceSatus( postStartActionsCollector, toStop, toStart, false );

            // Disabling plugins that need to.
            foreach( var disableC in toStop )
            {
                if( disableC.MustDisable )
                {
                    disableC.Plugin.Disable( _serviceHost );
                    var impact = disableC.ServiceImpact;
                    while( impact != null && !impact.Starting )
                    {
                        if( impact.Service.Implementation == disableC.Plugin )
                        {
                            impact.Service.SetPluginImplementation( null );
                        }
                        impact = impact.ServiceGeneralization;
                    }
                }
            }
            return new Result( errors.AsReadOnlyList() );
        }

        private void CancelSuccessfulStartForDisabled( List<StStartContext> toStart )
        {
            foreach( var disableC in toStart )
            {
                if( disableC.WasDisabled ) disableC.Plugin.Disable( _serviceHost );
            }
        }

        static void CancelSuccessfulPreStop( Dictionary<object, object> sharedMemory, List<StStopContext> successfulPreStop )
        {
            var cStart = new CancelPresStopContext( sharedMemory );
            foreach( var c in successfulPreStop )
            {
                if( !c.IsDisabledOnly && c.Success )
                {
                    c.Plugin.Status = PluginStatus.Started;
                    var rev = c.RollbackAction;
                    if( rev != null ) rev( cStart );
                    else c.Plugin.RealPluginObject.Start( cStart );
                }
            }
        }

        static void SetServiceSatus(
            Action<Action<IYodiiEngine>> postStartActionsCollector, 
            List<StStopContext> toStop, 
            List<StStartContext> toStart, 
            bool isTransition )
        {
            foreach( var stopC in toStop )
            {
                Debug.Assert( stopC.Success );
                var impact = stopC.ServiceImpact;
                while( impact != null )
                {
                    if( impact.SwappedImplementation != null )
                    {
                        Debug.Assert( impact.Implementation == stopC );
                        if( !impact.Implementation.HotSwapped && isTransition )
                        {
                            impact.Service.Status = ServiceStatus.StoppingSwapped;
                            impact.Service.RaiseStatusChanged( postStartActionsCollector, impact.SwappedImplementation.Plugin );
                        }
                    }
                    else
                    {
                        impact.Service.Status = isTransition ? ServiceStatus.Stopping : ServiceStatus.Stopped;
                        impact.Service.RaiseStatusChanged( postStartActionsCollector );
                    }
                    impact = impact.ServiceGeneralization;
                }
            }
            // Sending Starting events...
            foreach( var startC in toStart )
            {
                Debug.Assert( startC.Success );
                var impact = startC.ServiceImpact;
                while( impact != null )
                {
                    if( impact.SwappedImplementation != null )
                    {
                        Debug.Assert( impact.SwappedImplementation == startC );
                        if( !impact.Implementation.HotSwapped )
                        {
                            if( isTransition )
                            {
                                impact.Service.Status = ServiceStatus.StartingSwapped;
                                impact.Service.RaiseStatusChanged( postStartActionsCollector, impact.Implementation.Plugin );
                            }
                            else
                            {
                                impact.Service.Status = ServiceStatus.StartedSwapped;
                                impact.Service.RaiseStatusChanged( postStartActionsCollector );
                            }
                        }
                    }
                    else
                    {
                        impact.Service.Status = isTransition ? ServiceStatus.Starting : ServiceStatus.Started;
                        impact.Service.RaiseStatusChanged( postStartActionsCollector );
                    }
                    impact = impact.ServiceGeneralization;
                }
            }
        }

        /// <summary>
        /// Gets or sets the object that sends <see cref="IServiceHost.EventCreating"/> and <see cref="IServiceHost.EventCreated"/>.
        /// </summary>
        public object EventSender
        {
            get { return _serviceHost.EventSender; }
            set { _serviceHost.EventSender = value; }
        }

        PluginProxy EnsureProxy( IPluginInfo pluginInfo )
        {
            PluginProxy result;
            if( _plugins.TryGetValue( pluginInfo.PluginFullName, out result ) )
            {
                // Updates the pluginInfo reference (when discovered again, IPluginInfo instances change).
                if( result.PluginInfo != pluginInfo )
                {
                    result.PluginInfo = pluginInfo;
                }
            }
            else
            {
                result = new PluginProxy( pluginInfo );
                _plugins.Add( pluginInfo.PluginFullName, result );
            }
            return result;
        }


    }
}
