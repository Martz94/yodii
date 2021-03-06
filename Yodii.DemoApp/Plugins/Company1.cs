#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.DemoApp\Plugins\Company1.cs) is part of CiviKey. 
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using Yodii.DemoApp.Examples.Plugins.Views;

namespace Yodii.DemoApp
{
    public class Company1 : MonoWindowPlugin, IBusiness
    {
        readonly IMarketPlaceService _marketPlace;
        readonly IDeliveryService _delivery;
        readonly string _name;
        ObservableCollection<ProductCompany1> _products;
        ObservableCollection<Tuple<IClientInfo, MarketPlace.Product>> _orders;
        
        public Company1( IMarketPlaceService marketPlace, IDeliveryService deliveryService/*, string name*/ )
            : base( true )
        {
            _marketPlace = marketPlace;
            _delivery = deliveryService;
            _name = /*name*/ "toto";
            _products = new ObservableCollection<ProductCompany1>();
            _orders = new ObservableCollection<Tuple<IClientInfo, MarketPlace.Product>>();
        }

        internal void AddNewProduct( string name, ProductCategory category, int price )
        {
            Debug.Assert( !string.IsNullOrEmpty( name ) );
            Debug.Assert( price > 0 );

            ProductCompany1 p = new ProductCompany1( name, category, price, DateTime.Now, this );
            _marketPlace.AddNewProduct( p );
            _products.Add( p );
        }
      
        public ObservableCollection<ProductCompany1> Products
        {
            get { return _products; }
        }

        public ObservableCollection<Tuple<IClientInfo, MarketPlace.Product>> Orders
        {
            get { return _orders; }
        }

        public IMarketPlaceService MarketPlace { get { return _marketPlace; } }

        public string Name 
        { 
            get 
            { 
                return _name; 
            } 
        }

        protected override Window CreateWindow()
        {
            Window = new Company1View()
            {
                DataContext = this
            };

            return Window;
        }

        public class ProductCompany1 : Yodii.DemoApp.MarketPlace.Product
        {
            public ProductCompany1( string name, ProductCategory category, int price, DateTime creationDate, Company1 company )
            {
                Name = name;
                ProductCategory = category;
                Price = price;
                CreationDate = creationDate;
                Company = company;
            }
        }

        public void HandleOrders()
        {
            foreach( Tuple<IClientInfo, MarketPlace.Product> order in _orders )
            {
                _delivery.Deliver( order );
            }
        }
        
        public bool NewOrder( IClientInfo clientInfo, MarketPlace.Product product = null )
        {
            Tuple<IClientInfo, MarketPlace.Product> order = new Tuple<IClientInfo, MarketPlace.Product>( clientInfo, product );
            if( _orders.Contains( order ) ) return false;
            _orders.Add( order );
            RaisePropertyChanged( "newOrder" );
            if( _orders.Count >= 3 )
                HandleOrders();
            return true;
        }
    }
}
