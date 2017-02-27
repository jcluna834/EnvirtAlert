using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Storage.Streams;
using Microsoft.Phone.Maps.Controls;
using System.Device.Location;
using EnvirAlert.Models;
using System.Windows.Shapes;
using System.Windows.Media;
using Windows.Devices.Geolocation;
using Microsoft.Phone.Maps.Services;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Maps.Toolkit;
using EnvirAlert.WSIOT;
using System.Windows.Threading;
using Microsoft.Phone.Net.NetworkInformation;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.Windows.Media.Imaging;
using System.IO;

namespace EnvirAlert.Views
{
    /// <summary>
    /// Código de vista geográfica de sensores, en donde se muestra un mapa
    /// en el cual se ubica a partir de las coordenadas del GPS y se muestran
    /// información de los dispositivos mas cercanos y coloreados dependiendo
    /// del grado de contaminación del aire cada intervalos de tiempo t.
    /// </summary>
    public partial class PagVistaGeograficaSensores : PhoneApplicationPage
    {
        #region DEFINICION DE VARIABLES
        /// Para alertas
        private Alerta miAlerta;
        private Boolean bandAlerta = false;
        private bool bandera;

        /// Coordenadas
        private double la; //(Latitud)
        private double lo; //(Longitud)

        public static String latitud = "";
        public static String longitud = "";

        private Pushpin pin;
        private String DispositivoCercano = "";

        /// Aislar datos
        public IsolatedStorageSettings datosAislados = IsolatedStorageSettings.ApplicationSettings;

        /// Servicio Web

        WSSemanticSearchSoapClient WSa = new WSSemanticSearchSoapClient();
        WSSemanticSearchSoapClient WSb = new WSSemanticSearchSoapClient();

        /// De configuracion

        private int tSegundosAntes = -15;
        private double rangoSeguridad = 0.02;      /// Rango para determinar si hay otros dispositivos que puedan generar alertas
        private double rangoConsulta = 1;          /// Rango de dispositivos a consultar
        private string paramConsulta = "universidad del cauca";     ///Consulta que se enviara por parametro al servicio web
        private int tSegundosConsultas = 30;        /// Tiempo en segundos para intervalos de teiempo que se actualiza la información
        private int tSegundosHayInternet = 4;       /// Tiempo en segundos para intervalos de timpo que se verifica conexión a internet


        ///y nos recupere la información de nuestros dispositivos

        /// Auxiliares
        private bool mensajeYaSeMostro = false;

        #endregion

        #region CONSTRUCTORES
        /// <summary>
        /// Se encarga de inicializar los compinentes, e iniciar actualizaciones
        /// periodicas para obtener las coordenadas y mostrar las alertas que
        /// corresponden durante la ejecución de esta vista.
        /// </summary>
        public PagVistaGeograficaSensores()
        {
            pin = null;
            bandera = false;

            InitializeComponent();      //Se inicializan componentes
            ObetenerFeedXively();       //Se inicializan a travez de este método actualizaciones periodicas durante la ejecución de esta vista
            Alerta.vibrar.Stop();
        }
        #endregion

        #region METODOS USADOS
        /// <summary>
        /// Este método se encarga de obtener la posición del GPS del dispositivo
        /// a partir del lugar que este este dispositivo.
        /// </summary>
        private async void miLocalizacion()
        {
            pin = new Pushpin();

            try
            {
                //PARA MANEJO DINAMICO 
                Geolocator myGeolocator = new Geolocator();
                Geoposition myGeoposition = await myGeolocator.GetGeopositionAsync();


                //la = Convert.ToDouble(myGeoposition.Coordinate.Latitude);
                //lo = Convert.ToDouble(myGeoposition.Coordinate.Longitude);
                la = 2.446479;
                lo = -76.598496;

                //Se pinta en el mapa nuestra posición
                pin.GeoCoordinate = new GeoCoordinate(la, lo);
                pin.Content = "Estas Aquí";
                pin.Background = new SolidColorBrush(Colors.Blue);

                if (bandera == false)
                {
                    this.mapWithMyLocation.Center = pin.GeoCoordinate;
                    this.mapWithMyLocation.ZoomLevel = 18;
                    bandera = true;
                }

                // Creacion de con contenedor MapLayer para pintar las posiciones
                // y estado de la calidad del aire en el mapa de los dispositivos
                // cercanos.
                MapOverlay myLocationOverlay = new MapOverlay();
                myLocationOverlay.Content = pin;
                myLocationOverlay.PositionOrigin = new Point(0.5, 0.5);
                myLocationOverlay.GeoCoordinate = pin.GeoCoordinate;

                MapLayer myLocationLayer = new MapLayer();
                myLocationLayer.Add(myLocationOverlay);
                mapWithMyLocation.Layers.Add(myLocationLayer);
            }
            finally
            {

            }


        }


        /// <summary>
        /// Permite la obtención y actualización de los FeedXively, con el proposito
        /// de mostrar la alerta, si es que la opción que permite mostrar alertas esta
        /// habilitada.
        /// </summary>
        private void ObetenerFeedXively()
        {

            //Invocación a 'actualizaciones_TimerTick' el cual consulta cada cieto intervalo
            //de tiempo al Indice Semántico por dispositivos cercanos y si es pertinente
            //se activan mensajes de alertas.
            DispatcherTimer dispatcherTimer;
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(actualizaciones_TimerTick);    //Se establece a quien se va a estar invocando
            dispatcherTimer.Interval = new TimeSpan(0, 0, tSegundosConsultas);                      //Se establece intervalos de tiempo (t = 30)
            dispatcherTimer.Start();                                                //Se inician las invocaciones

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


        /// <summary>
        /// Este método es invocado desde un ojeto DispatcherTimer cada cierto intervalo de tiempo
        /// y que Si esta activada la opción de 'Activar Alertas' entonces se consulta al Indice
        /// Semántico y se recuperan los dispositivos mas cercanos, son procesados los datos y 
        /// si es pertinente se muetra una alarma.
        /// </summary>
        /// <param name="sender">Objeto enviante.</param>
        /// <param name="e">Resultados del evento</param>
        private void actualizaciones_TimerTick(object sender, EventArgs e)
        {
            try
            {
                if (HayConexiónInternet())
                {
                    WSb.RetornarMapaLugarDatapointsAsync(la, lo, paramConsulta, DateTime.Now.AddSeconds(tSegundosAntes), DateTime.Now, "Español", rangoSeguridad);
                    WSb.RetornarMapaLugarDatapointsCompleted += ob1_RetornarMapaLugarDatapointsCompleted;
                    WSa.RetornarMapaLugarDatapointsAsync(la, lo, paramConsulta, DateTime.Now.AddSeconds(tSegundosAntes), DateTime.Now, "Español", rangoConsulta);
                    WSa.RetornarMapaLugarDatapointsCompleted += ob_RetornarMapaLugarDatapointsCompleted;
                }

            }
            finally
            {
                /// En caso de que haya existido algun error de conexión, se intenta establecer 
                /// nuevamente la conexión
                if (HayConexiónInternet())
                {
                    WSa = new WSSemanticSearchSoapClient();
                    WSb = new WSSemanticSearchSoapClient();
                }

            }
        }

        /// <summary>
        /// Se recupera los feedXively que corresponden a los parametros de la busqueda indicados
        /// posteriormente se analiza la información y si se muestra la alerta de como es
        /// la calidad del aire en la proximidad de un radio determinado.
        /// </summary>
        /// <param name="sender">Objeto enviante</param>
        /// <param name="e">Resultados</param>
        void ob1_RetornarMapaLugarDatapointsCompleted(object sender, RetornarMapaLugarDatapointsCompletedEventArgs e)
        {
            FeedXively[] arrayFeeds = e.Result;
            try
            {
                if (bandAlerta == true)
                {
                    miAlerta.cancelar();
                }

                bandAlerta = false;
                tblInfoPeligro.Text = "";
                for (int i = 0; i < arrayFeeds.Length; i++)
                {

                    PagListaSensores color = new PagListaSensores();
                    if (color.riesgo(arrayFeeds[i].feed) == 3)
                    {
                        bandAlerta = true;
                        DispositivoCercano = arrayFeeds[i].feed.id;

                    }
                }


                if (bandAlerta == true)
                {
                    miAlerta = new Alerta();
                    miAlerta.iniciar(mediaControl);
                }
            }
            finally
            {
            }
        }


        /// <summary>
        /// Método asincrono donde recupera los feedXively que corresponden a los parametros de la busqueda indicados
        /// posteriormente se analiza la información y se pinta en el mapa.
        /// </summary>
        /// <param name="sender">Objeto enviante</param>
        /// <param name="e">Resultados</param>
        public void ob_RetornarMapaLugarDatapointsCompleted(object sender, RetornarMapaLugarDatapointsCompletedEventArgs e)
        {
            /// Se recupera el resultado de la busqueda
            FeedXively[] arrayFeeds = e.Result;

            mapWithMyLocation.Layers.Clear();//Para borrar las capas que hay en el momento para cuando se actualiza
            /*if (bandAlerta == true)
                miAlerta.cancelar();
            bandAlerta = false;*/

            try
            {
                if (bandera == false)
                {
                    this.mapWithMyLocation.Center = pin.GeoCoordinate;
                    this.mapWithMyLocation.ZoomLevel = 20;
                }
            }
            catch (Exception) { }
            finally { }

            try
            {

                for (int i = 0; i < arrayFeeds.Length; i++)
                {

                    //Se crea un nuevo PushPin en las coordenadas de un Dispositivo
                    pin = new Pushpin();
                    pin.GeoCoordinate = new GeoCoordinate(double.Parse(arrayFeeds[i].feed.location.lat/*.Replace(".", ",")*/), double.Parse(arrayFeeds[i].feed.location.lon/*.Replace(".", ",")*/));

                    //////////////Logica para asignar contenido y color
                    PagListaSensores color = new PagListaSensores();
                    if (color.riesgo(arrayFeeds[i].feed) == 1)
                    {
                        pin.Content = "D" + (i + 1) + ": Aire puro";
                        pin.Background = new SolidColorBrush(Colors.Green);
                    }
                    else if (color.riesgo(arrayFeeds[i].feed) == 2)
                    {
                        pin.Content = "D" + (i + 1) + ": Precaucion\n";             // +"Aire riesgoso";
                        pin.Background = new SolidColorBrush(Colors.Orange);
                    }
                    else if (color.riesgo(arrayFeeds[i].feed) == 3)
                    {
                        pin.Content = "D" + (i + 1) + ": Peligro\n";                // +"Aire contaminado";
                        pin.Background = new SolidColorBrush(Colors.Red);
                        //bandAlerta = true;
                        if (arrayFeeds[i].feed.id.Equals(DispositivoCercano))
                        {
                            textoAlerta.Text = "Alerta! Está en zona contaminada:";
                            tblInfoPeligro.Text = "\n" + "D" + (i + 1) + ": " + arrayFeeds[i].feed.location.name;
                        }
                    }
                    pin.Name = arrayFeeds[i].feed.id;
                    pin.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(click);
                    //////////////////////////////////////////////////////

                    /// Se añaden al mapa cada PushPin
                    MapOverlay myLocationOverlay = new MapOverlay();
                    myLocationOverlay.Content = pin;
                    myLocationOverlay.PositionOrigin = new Point(0.5, 0.5);
                    myLocationOverlay.GeoCoordinate = pin.GeoCoordinate;

                    MapLayer myLocationLayer = new MapLayer();
                    myLocationLayer.Add(myLocationOverlay);
                    mapWithMyLocation.Layers.Add(myLocationLayer);
                }
            }
            finally
            {
                miLocalizacion();       //Se establece nuevamente mi ubicación
            }
        }

        #endregion

        #region METODOS/EVENTOS DE REDIRECCIONAMIENTO A OTRAS VISTAS

        /// <summary>
        /// Evento, en el cual se redirecciona a PagInfoSensores con un parametro
        /// el cual es el feedID para mostrar el contenido
        /// </summary>
        /// <param name="sender">Objeto que invoca</param>
        /// <param name="e">Resultado de invocación</param>
        void click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Pushpin miPin = (Pushpin)sender; ;
            try
            {
                if (bandAlerta == true)
                {
                    miAlerta.cancelar();
                }
                NavigationService.Navigate(new Uri("/Views/PagInfoSensores.xaml?feedId=" + miPin.Name, UriKind.Relative));
            }
            catch (Exception exeption)
            {
            }
            finally ///Finalmente nos interesa que se nos redireccione.
            {
                NavigationService.Navigate(new Uri("/Views/PagInfoSensores.xaml?feedId=" + miPin.Name, UriKind.Relative));
            }

        }



        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            base.OnBackKeyPress(e);
            try
            {
                if (bandAlerta)
                {
                    miAlerta.cancelar();
                }
            }
            finally ///Finalmente interesa que si hay un error solamente no se gestione el error para evitar
            ///Bloqueos
            {

            }


        }


        void query_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Error == null)
            {
                ObservableCollection<MapLocation> Results = new ObservableCollection<MapLocation>(e.Result);
            }
        }

        private void UbicarmeClick(object sender, RoutedEventArgs e)
        {
            Pushpin ubicarme = new Pushpin();
            try
            {
                ubicarme.GeoCoordinate = new GeoCoordinate(la, lo);
                this.mapWithMyLocation.Center = ubicarme.GeoCoordinate;
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