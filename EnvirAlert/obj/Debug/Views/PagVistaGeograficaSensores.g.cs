﻿#pragma checksum "C:\Users\Yamid\Desktop\EnvirAlert_Version_3.10-MedioDocumentada\EnvirAlert_Version_3.10-MedioDocumentada\EnvirAlert\Views\PagVistaGeograficaSensores.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "4E9CCF8DCC92E5A4047098E85A0EF09E"
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
using Microsoft.Phone.Maps.Controls;
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
    
    
    public partial class PagVistaGeograficaSensores : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.StackPanel TitlePanel;
        
        internal System.Windows.Controls.TextBlock textoEstaAqui;
        
        internal System.Windows.Controls.MediaElement mediaControl;
        
        internal Microsoft.Phone.Maps.Controls.Map mapWithMyLocation;
        
        internal System.Windows.Controls.TextBlock textoAlerta;
        
        internal System.Windows.Controls.TextBlock tblInfoPeligro;
        
        internal System.Windows.Controls.Image imgEstadoConexion;
        
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
            System.Windows.Application.LoadComponent(this, new System.Uri("/EnvirAlert;component/Views/PagVistaGeograficaSensores.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.TitlePanel = ((System.Windows.Controls.StackPanel)(this.FindName("TitlePanel")));
            this.textoEstaAqui = ((System.Windows.Controls.TextBlock)(this.FindName("textoEstaAqui")));
            this.mediaControl = ((System.Windows.Controls.MediaElement)(this.FindName("mediaControl")));
            this.mapWithMyLocation = ((Microsoft.Phone.Maps.Controls.Map)(this.FindName("mapWithMyLocation")));
            this.textoAlerta = ((System.Windows.Controls.TextBlock)(this.FindName("textoAlerta")));
            this.tblInfoPeligro = ((System.Windows.Controls.TextBlock)(this.FindName("tblInfoPeligro")));
            this.imgEstadoConexion = ((System.Windows.Controls.Image)(this.FindName("imgEstadoConexion")));
        }
    }
}
