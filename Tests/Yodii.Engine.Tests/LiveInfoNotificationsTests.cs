#region LGPL License
/*----------------------------------------------------------------------------
* This file (Tests\Yodii.Engine.Tests\LiveInfoNotificationsTests.cs) is part of CiviKey. 
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
using NUnit.Framework;
using Yodii.Engine.Tests.Mocks;
using Yodii.Model;

namespace Yodii.Engine.Tests
{
    [TestFixture]
    public class LiveInfoNotificationsTests
    {
        [Test]
        public void ConfigChanged()
        {
            /**
             *  +--------+
             *  |ServiceA+ ------+
             *  |        |       |
             *  +---+----+       |
             *      |            |
             *      |            |
             *      |            |
             *      |        +---+---*-+
             *  +---+-----+  |PluginA-2|
             *  |PluginA-1|  |         |
             *  |         |  +---------+
             *  +---------+
             */
            DiscoveredInfo info = MockInfoFactory.ServiceWithTwoPlugins();
            YodiiEngine engine = new YodiiEngine( new BuggyYodiiEngineHostMock() );
            engine.Configuration.SetDiscoveredInfo( info );
            engine.StartEngine();
            ILiveServiceInfo sA = engine.LiveInfo.FindService( "ServiceA" );
            ILivePluginInfo p1 = engine.LiveInfo.FindPlugin( "PluginA-1" );
            ILivePluginInfo p2 = engine.LiveInfo.FindPlugin( "PluginA-2" );

            Assert.That( sA != null && p1 != null && p2 != null );

            Assert.That( p1.Capability.CanStart && p2.Capability.CanStart && sA.Capability.CanStart, Is.True );
            Assert.That( p1.Capability.CanStartWithFullStart && p2.Capability.CanStartWithFullStart && sA.Capability.CanStartWithFullStart, Is.True );
            Assert.That( p1.Capability.CanStartWithStartRecommended && p2.Capability.CanStartWithStartRecommended && sA.Capability.CanStartWithStartRecommended, Is.True );

            HashSet<string> propertyChanged = new HashSet<string>();
            p1.PropertyChanged += ( s, e ) => 
                                    { 
                                        Assert.That( s, Is.SameAs( p1 ) ); 
                                        Assert.That( propertyChanged.Add( "p1."+e.PropertyName ) );
                                    };
            p1.Capability.PropertyChanged += ( s, e ) => 
                                                { 
                                                    Assert.That( s, Is.SameAs( p1.Capability ) ); 
                                                    Assert.That( propertyChanged.Add( "p1.Capablity."+e.PropertyName ) );
                                                };
            p2.PropertyChanged += ( s, e ) => 
                                    { 
                                        Assert.That( s, Is.SameAs( p2 ) ); 
                                        Assert.That( propertyChanged.Add( "p2."+e.PropertyName ) );
                                    };
            p2.Capability.PropertyChanged += ( s, e ) => 
                                                { 
                                                    Assert.That( s, Is.SameAs( p2.Capability ) ); 
                                                    Assert.That( propertyChanged.Add( "p2.Capablity."+e.PropertyName ) );
                                                };
            sA.PropertyChanged += ( s, e ) => 
                                    { 
                                        Assert.That( s, Is.SameAs( sA ) ); 
                                        Assert.That( propertyChanged.Add( "sA."+e.PropertyName ) );
                                    };
            sA.Capability.PropertyChanged += ( s, e ) => 
                                                { 
                                                    Assert.That( s, Is.SameAs( sA.Capability ) ); 
                                                    Assert.That( propertyChanged.Add( "sA.Capablity."+e.PropertyName ) );
                                                };

            IConfigurationLayer config = engine.Configuration.Layers.Create( "Default" );
            config.Items.Set( p1.FullName, ConfigurationStatus.Disabled );
            
            Assert.That( p1.Capability.CanStart 
                        && p1.Capability.CanStartWithFullStart 
                        && p1.Capability.CanStartWithStartRecommended, Is.False );

            Assert.That( p2.Capability.CanStart && sA.Capability.CanStart, Is.True );
            Assert.That( p2.Capability.CanStartWithFullStart && sA.Capability.CanStartWithFullStart, Is.True );
            Assert.That( p2.Capability.CanStartWithStartRecommended && sA.Capability.CanStartWithStartRecommended, Is.True );

            CollectionAssert.AreEquivalent( new string[]{
                "p1.Capablity.CanStart", 
                "p1.Capablity.CanStartWithFullStart", 
                "p1.Capablity.CanStartWithStartRecommended", 
                "p1.DisabledReason", 
                "p1.RunningStatus", 
                "p1.ConfigOriginalStatus", 
                "p1.WantedConfigSolvedStatus",
                "p1.FinalConfigSolvedStatus" }, propertyChanged );
            propertyChanged.Clear();

            config.Items.Set( p1.FullName, ConfigurationStatus.Optional );

            CollectionAssert.AreEquivalent( new string[]{
                "p1.Capablity.CanStart", 
                "p1.Capablity.CanStartWithFullStart", 
                "p1.Capablity.CanStartWithStartRecommended", 
                "p1.DisabledReason", 
                "p1.RunningStatus", 
                "p1.ConfigOriginalStatus", 
                "p1.WantedConfigSolvedStatus",
                "p1.FinalConfigSolvedStatus" }, propertyChanged );
        }

    }
}
