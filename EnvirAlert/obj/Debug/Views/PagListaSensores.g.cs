﻿#pragma checksum "C:\Users\Yamid\Desktop\EnvirAlert_Version_3.10-MedioDocumentada\EnvirAlert_Version_3.10-MedioDocumentada\EnvirAlert\Views\PagListaSensores.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "B748F9E8E0076E1587846E7C8672D988"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace EnvirAlert.Views {
    
    
    public partial class PagListaSensores : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.Image imgEstadoConexion;
        
        internal System.Windows.Controls.Grid ContentPanel;
        
        internal System.Windows.Controls.TextBlock txtTitulo;
        
        internal System.Windows.Controls.Grid listaDispositivos;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/EnvirAlert;component/Views/PagListaSensores.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.imgEstadoConexion = ((System.Windows.Controls.Image)(this.FindName("imgEstadoConexion")));
            this.ContentPanel = ((System.Windows.Controls.Grid)(this.FindName("ContentPanel")));
            this.txtTitulo = ((System.Windows.Controls.TextBlock)(this.FindName("txtTitulo")));
            this.listaDispositivos = ((System.Windows.Controls.Grid)(this.FindName("listaDispositivos")));
        }
    }
}

