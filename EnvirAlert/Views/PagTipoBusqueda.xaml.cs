using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using EnvirAlert.WSIOT;
using Windows.Devices.Geolocation;
using System.Device.Location;
using EnvirAlert.Models;
using Microsoft.Phone.Net.NetworkInformation;
using System.Windows.Threading;
using System.IO;
using System.Windows.Media.Imaging;

namespace EnvirAlert.Views
{
    /// <summary>
    /// Clase de la vista PagTipoBusqueda en la cual es posible de buscar un dispositivo
    /// por un FeedId o Lugar
    /// </summary>
    public partial class PagTipoBusqueda : PhoneApplicationPage
    {
        #region DEFINICION DE VARIABLES
        //Resultados de consultas
        private FeedXively[] arrayFeeds;

        /// De configuracion

        private int tSegundosAntes = -15;
        private double rangoSeguridad = 0.02;      /// Rango para determinar si hay otros dispositivos que puedan generar alertas
        private double rangoConsulta = 1;          /// Rango de dispositivos a consultar
        private string paramConsulta = "universidad del cauca";     ///Consulta que se enviara por parametro al servicio web
        private int tSegundosConsultas = 30;        /// Tiempo en segundos para intervalos de teiempo que se actualiza la información
        private int tSegundosHayInternet = 4;       /// Tiempo en segundos para intervalos de timpo que se verifica conexión a internet


        /// Auxiliares
        private bool mensajeYaSeMostro = false;
        #endregion

        #region CONSTRUCTORES

        /// <summary>
        /// Inicialización de componentes y obtención de datos.
        /// </summary>
        public PagTipoBusqueda()
        {
            InitializeComponent();
            ObetenerFeedXively();
            arrayFeeds = null;


            DispatcherTimer timerConexionInternet;
            timerConexionInternet = new System.Windows.Threading.DispatcherTimer();
            timerConexionInternet.Tick += new EventHandler(ConexionInternet_TimerTick);    //Se establece a quien se va a estar invocando
            timerConexionInternet.Interval = new TimeSpan(0, 0, tSegundosHayInternet);                      //Se establece intervalos de tiempo (t = 4)
            timerConexionInternet.Start();
        }


        /// <summary>   Event handler. Called by ConexionInternet for timer tick events. </summary>
        /// <param name="sender">   Objeto enviante. </param>
        /// <param name="e">        Informacion del evento. </param>
        private void ConexionInternet_TimerTick(object sender, EventArgs e)
        {
            if (HayConexiónInternet())
            {
                string recurso = Directory.GetCurrentDirectory() + @"/Images/conInternet.png";
                imgEstadoConexion.Source = new BitmapImage(new Uri(recurso));

            }
            else
            {
                string recurso = Directory.GetCurrentDirectory() + @"/Images/sinInternet.png";
                imgEstadoConexion.Source = new BitmapImage(new Uri(recurso));
            }
        }
        #endregion

        #region METODOS USADOS

        /// <summary>
        /// Se encarga de obtener los FeedXively en un radio de 1km.
        /// </summary>
        private async void ObetenerFeedXively()
        {
            /// Que esten muchas condiciones anidadas es porque cada llamado se demora su tiempo, puede
            /// que se pierda la conexión en algun momento.
            if (HayConexiónInternet())
            {
                Geolocator myGeolocator = new Geolocator();
                Geoposition myGeoposition = await myGeolocator.GetGeopositionAsync();
                if (HayConexiónInternet())
                {
                    Double la = Convert.ToDouble(myGeoposition.Coordinate.Latitude);
                    Double lo = Convert.ToDouble(myGeoposition.Coordinate.Longitude);

                    Geocoordinate myGeocoordinate = myGeoposition.Coordinate;
                    GeoCoordinate myGeoCoordinate = CoordinateConverter.ConvertGeocoordinate(myGeocoordinate);

                    if (HayConexiónInternet())
                    {
                        WSSemanticSearchSoapClient WS = new WSSemanticSearchSoapClient();
                        WS.RetornarMapaLugarDatapointsAsync(la, lo, paramConsulta, DateTime.Now.AddSeconds(tSegundosAntes), DateTime.Now, "Español", rangoConsulta);
                        WS.RetornarMapaLugarDatapointsCompleted += ob_RetornarMapaLugarDatapointsCompleted;
                    }
                }

            }
        }


        /// <summary>
        /// Contiene los resultados de la consulta al indice semántico y carga las listas para
        /// desplegar los dispositivos en la pantalla.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ob_RetornarMapaLugarDatapointsCompleted(object sender, RetornarMapaLugarDatapointsCompletedEventArgs e)
        {
            arrayFeeds = e.Result;

            try
            {
                string[] lista = new string[arrayFeeds.Length];
                string[] listaFeedIDs = new string[arrayFeeds.Length];

                for (int i = 0; i < arrayFeeds.Length; i++)
                {
                    lista[i] = arrayFeeds[i].feed.location.name;
                    listaFeedIDs[i] = arrayFeeds[i].feed.id;
                }

                this.LP.ItemsSource = lista;
                this.LP_FeedID.ItemsSource = listaFeedIDs;
                txtEspera.Text = "";
                txtEsperaFeed.Text = "";
                btnBuscar.Visibility = System.Windows.Visibility.Visible;  
            }
            finally
            {

            }
                     
        }

        #endregion

        #region EVENTOS

        private void rbtnUbicacion_Checked(object sender, RoutedEventArgs e)
        {
            panelPorUbicacion.Visibility = System.Windows.Visibility.Visible;
            panelPorFeedID.Visibility = System.Windows.Visibility.Collapsed;           
        }

        private void rbtnFeedID_Checked(object sender, RoutedEventArgs e)
        {
            panelPorUbicacion.Visibility = System.Windows.Visibility.Collapsed;
            panelPorFeedID.Visibility = System.Windows.Visibility.Visible;
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (rbtnUbicacion.IsChecked == true)
                {
                    for (int i = 0; i < arrayFeeds.Length; i++)
                    {
                        if (this.LP.SelectedItem.Equals(arrayFeeds[i].feed.location.name))
                        {
                            NavigationService.Navigate(new Uri("/Views/PagInfoSensores.xaml?feedId=" + arrayFeeds[i].feed.id, UriKind.Relative));
                        }
                    }
                }

                if (rbtnFeedID.IsChecked == true)
                {
                    for (int i = 0; i < arrayFeeds.Length; i++)
                    {
                        if (this.LP_FeedID.SelectedItem.Equals(arrayFeeds[i].feed.id))
                        {
                            NavigationService.Navigate(new Uri("/Views/PagInfoSensores.xaml?feedId=" + arrayFeeds[i].feed.id, UriKind.Relative));
                        }
                    }
                }
            }
            finally
            {

            }
           
        }
        #endregion

        #region VALIDACION DEL SISTEMA

        /// <summary>   Válida si hay o no hay una conexión  a internet  </summary>
        ///
        /// <returns>   Verdadero si hay conexión a internet, sino falso </returns>
        private bool HayConexiónInternet()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                mensajeYaSeMostro = false;
                return true;
            }
            else
            {
                if (!mensajeYaSeMostro)
                {
                    MessageBox.Show("Se ha perdido la conexión a internet, intentalo en un momento", "Sin Conexión", MessageBoxButton.OK);
                    mensajeYaSeMostro = true;
                }
            }
            return false;
        }

        #endregion
    }
}