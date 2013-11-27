﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace Yodii.Model
{
    public class FinalConfiguration
    {
        readonly CKSortedArrayKeyList<FinalConfigurationItem, string> _items;

        public IReadOnlyList<FinalConfigurationItem> Items
        {
            get { return _items; }
        }

        public ConfigurationStatus GetStatus( string serviceOrPluginId )
        {
            Debug.Assert( ConfigurationStatus.Optional == 0 );
            return _items.GetByKey( serviceOrPluginId ).Status;
        }

        public FinalConfiguration( Dictionary<string, ConfigurationStatus> finalStatus )
        {
            _items = new CKSortedArrayKeyList<FinalConfigurationItem, string>( e => e.ServiceOrPluginId, ( x, y ) => StringComparer.Ordinal.Compare( x, y ) );
            foreach( var item in finalStatus ) _items.Add( new FinalConfigurationItem( item.Key, item.Value ) );
        }
    }
}