﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Fluent;
using System.Timers;
using System.Threading;
using Yodii.Model;
using System.Diagnostics;
using Yodii.Lab.ConfigurationEditor;
using Yodii.Lab.Mocks;

namespace Yodii.Lab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        readonly MainWindowViewModel _vm;
        ConfigurationEditorWindow _activeConfEditorWindow = null;

        public MainWindow()
        {
            _vm = new MainWindowViewModel();
            this.DataContext = _vm;


            /**
             * Graph display example.
             */
            IServiceInfo serviceA = _vm.CreateNewService( "Service.A" );
            IServiceInfo serviceB = _vm.CreateNewService( "Service.B" );
            IServiceInfo serviceAx = _vm.CreateNewService( "Service.Ax", serviceA );

            IPluginInfo pluginA1 = _vm.CreateNewPlugin( Guid.NewGuid(), "Plugin.A1", serviceA );
            IPluginInfo pluginA2 = _vm.CreateNewPlugin( Guid.NewGuid(), "Plugin.A2", serviceA );
            _vm.SetPluginDependency( pluginA2, serviceB, RunningRequirement.Running );
            IPluginInfo pluginB1 = _vm.CreateNewPlugin( Guid.NewGuid(), "Plugin.B1", serviceB );
            IPluginInfo pluginAx1 = _vm.CreateNewPlugin( Guid.NewGuid(), "Plugin.Ax1", serviceAx );

            InitializeComponent();
        }

        private void ConfEditorButton_Click( object sender, RoutedEventArgs e )
        {
            if( _activeConfEditorWindow != null )
            {
                _activeConfEditorWindow.Activate();
            }
            else
            {
                _activeConfEditorWindow = new ConfigurationEditorWindow( _vm.ConfigurationManager );
                _activeConfEditorWindow.Closing += ( s, e2 ) => { _activeConfEditorWindow = null; };

                _activeConfEditorWindow.Show();
            }
        }

        private void NewGraphLayoutButton_Click( object sender, RoutedEventArgs e )
        {
            MenuItem item = e.OriginalSource as MenuItem;

            String newSelection = item.DataContext as String;

            this.graphLayout.LayoutAlgorithmType = newSelection;
        }

        private void ReorderGraphButton_Click( object sender, RoutedEventArgs e )
        {
            string oldLayout = this.graphLayout.LayoutAlgorithmType;

            this.graphLayout.LayoutAlgorithmType = null;

            this.graphLayout.LayoutAlgorithmType = oldLayout;
        }

        private void StackPanel_MouseDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            FrameworkElement vertexPanel = sender as FrameworkElement;

            YodiiGraphVertex vertex = vertexPanel.DataContext as YodiiGraphVertex;

            _vm.SelectedVertex = vertex;
        }

        private void graphLayout_MouseDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            _vm.SelectedVertex = null;
        }

        private void NewServiceButton_Click( object sender, RoutedEventArgs e )
        {
            IServiceInfo selectedService = null;

            if( _vm.SelectedVertex != null )
            {
                if( _vm.SelectedVertex.IsService )
                {
                    selectedService = _vm.SelectedVertex.LiveServiceInfo.ServiceInfo;
                }
                else if( _vm.SelectedVertex.IsPlugin )
                {
                    selectedService = _vm.SelectedVertex.LivePluginInfo.PluginInfo.Service;
                }
            }

            AddServiceWindow window = new AddServiceWindow( _vm.ServiceInfos, selectedService );

            window.NewServiceCreated += ( s, nse ) =>
            {
                if( _vm.ServiceInfos.Any( si => si.ServiceFullName == nse.ServiceName ) )
                {
                    nse.CancelReason = String.Format( "Service with name {0} already exists. Pick another name.", nse.ServiceName );
                }
                else
                {
                    IServiceInfo newService = _vm.CreateNewService( nse.ServiceName, nse.Generalization );
                    _vm.SelectService( newService );
                }
            };

            window.Owner = this;

            window.ShowDialog();
        }

        private void NewPluginButton_Click( object sender, RoutedEventArgs e )
        {
            IServiceInfo selectedService = null;

            if( _vm.SelectedVertex != null )
            {
                if( _vm.SelectedVertex.IsService )
                {
                    selectedService = _vm.SelectedVertex.LiveServiceInfo.ServiceInfo;
                }
                else if( _vm.SelectedVertex.IsPlugin )
                {
                    selectedService = _vm.SelectedVertex.LivePluginInfo.PluginInfo.Service;
                }
            }

            AddPluginWindow window = new AddPluginWindow( _vm.ServiceInfos, selectedService );

            window.NewPluginCreated += ( s, npe ) =>
            {
                if( _vm.PluginInfos.Any( si => si.PluginId == npe.PluginId ) )
                {
                    npe.CancelReason = String.Format( "Plugin with GUID {0} already exists. Pick another GUID.", npe.PluginId.ToString() );
                }
                else
                {
                    IPluginInfo newPlugin = _vm.CreateNewPlugin( npe.PluginId, npe.PluginName, npe.Service );
                    foreach( var kvp in npe.ServiceReferences )
                    {
                        _vm.SetPluginDependency( newPlugin, kvp.Key, kvp.Value );
                    }
                    _vm.SelectPlugin( newPlugin );
                }
            };

            window.Owner = this;

            window.ShowDialog();
        }
    }
}