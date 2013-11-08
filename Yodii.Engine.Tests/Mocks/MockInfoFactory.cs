﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yodii.Model;

namespace Yodii.Engine.Tests.Mocks
{
    internal static class MockInfoFactory
    {
        public static DiscoveredInfo CreateGraph001()
        {
            /**
             *                 +--------+                              +--------+
             *     +---------->|ServiceA+-------+   *----------------->|ServiceB|
             *     |           +---+----+       |   | Need Running     +---+----+
             *     |               |            |   |                      |
             *     |               |            |   |                      |
             *     |               |            |   |                      |
             *     |               |            |   |                      |
             * +---+-----+     +---+-----+  +---+---*-+                +---+-----+
             * |ServiceAx|     |PluginA-1|  |PluginA-2|                |PluginB-1|
             * +----+----+     +---------+  +---------+                +---------+
             *      |
             *      |
             * +----+-----+
             * |PluginAx-1|
             * +----------+
             */

            var d = new DiscoveredInfo();

            d.ServiceInfos.Add( new ServiceInfo( "ServiceA", d.DefaultAssembly ) );
            d.ServiceInfos.Add( new ServiceInfo( "ServiceB", d.DefaultAssembly ) );
            d.ServiceInfos.Add( new ServiceInfo( "ServiceAx", d.DefaultAssembly ) );
            d.FindService( "ServiceAx" ).Generalization = d.FindService( "ServiceA" );

            d.PluginInfos.Add( new PluginInfo( "PluginA-1", d.DefaultAssembly ) );
            d.PluginInfos.Add( new PluginInfo( "PluginA-2", d.DefaultAssembly ) );
            d.PluginInfos.Add( new PluginInfo( "PluginAx-1", d.DefaultAssembly ) );
            d.PluginInfos.Add( new PluginInfo( "PluginB-1", d.DefaultAssembly ) );

            d.FindPlugin( "PluginA-1" ).Service = d.FindService( "ServiceA" );
            d.FindPlugin( "PluginA-2" ).Service = d.FindService( "ServiceA" );
            d.FindPlugin( "PluginAx-1" ).Service = d.FindService( "ServiceAx" );
            d.FindPlugin( "PluginB-1" ).Service = d.FindService( "ServiceB" );

            d.FindPlugin( "PluginA-2" ).AddServiceReference( d.FindService( "ServiceB" ), RunningRequirement.Running );

            return d;
        }

        public static DiscoveredInfo CreateGraph002()
        {
            /**
             *                 +--------+                              +--------+
             *     +---------->|ServiceA+-------+   *----------------->|ServiceB|
             *     |           +---+----+       |   | Need Running     +---+----+
             *     |               |            |   |                      |
             *     |               |            |   |                      |
             *     |               |            |   |                      |
             *     |               |            |   |                      |
             * +---+-----+     +---+-----+  +---+---*-+                +---+-----+
             * |ServiceAx|     |PluginA-1|  |PluginA-2|                |PluginB-1|
             * +----+----+     +---------+  +---------+                +---------+
             *      |
             *      |
             *      |--------|
             *      |   +----+-----+
             *      |   |PluginAx-1|
             *      |   +----------+
             *      |        
             *      |        
             *  +---+------+  
             *  |ServiceAxx|  
             *  +----+-----+  
             * 
             * 
             */

            var d = new DiscoveredInfo();

            d.ServiceInfos.Add( new ServiceInfo( "ServiceA", d.DefaultAssembly ) );
            d.ServiceInfos.Add( new ServiceInfo( "ServiceB", d.DefaultAssembly ) );
            d.ServiceInfos.Add( new ServiceInfo( "ServiceAx", d.DefaultAssembly ) );
            d.ServiceInfos.Add( new ServiceInfo( "ServiceAxx", d.DefaultAssembly ) );
            d.FindService( "ServiceAx" ).Generalization = d.FindService( "ServiceA" );
            d.FindService( "ServiceAxx" ).Generalization = d.FindService( "ServiceAx" );

            d.PluginInfos.Add( new PluginInfo( "PluginA-1", d.DefaultAssembly ) );
            d.PluginInfos.Add( new PluginInfo( "PluginA-2", d.DefaultAssembly ) );
            d.PluginInfos.Add( new PluginInfo( "PluginAx-1", d.DefaultAssembly ) );
            //d.PluginInfos.Add( new PluginInfo( "PluginAxx-1", d.DefaultAssembly ) );
            d.PluginInfos.Add( new PluginInfo( "PluginB-1", d.DefaultAssembly ) );

            d.FindPlugin( "PluginA-1" ).Service = d.FindService( "ServiceA" );
            d.FindPlugin( "PluginA-2" ).Service = d.FindService( "ServiceA" );
            d.FindPlugin( "PluginAx-1" ).Service = d.FindService( "ServiceAx" );
            //d.FindPlugin( "PluginAxx-1" ).Service = d.FindService( "ServiceAxx" );
            d.FindPlugin( "PluginB-1" ).Service = d.FindService( "ServiceB" );

            d.FindPlugin( "PluginA-2" ).AddServiceReference( d.FindService( "ServiceB" ), RunningRequirement.Running );

            return d;
        }
    }
}