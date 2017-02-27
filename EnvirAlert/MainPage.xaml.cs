using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using EnvirAlert.Resources;
using Microsoft.Devices;
using EnvirAlert.Models;
using Windows.Devices.Geolocation;
using System.Windows.Threading;
using EnvirAlert.WSIOT;
using EnvirAlert.Views;
using Microsoft.Xna.Framework.GamerServices;
using System.Diagnostics;
using Microsoft.Phone.Net.NetworkInformation;
using System.IO;
using System.Windows.Media.Imaging;

namespace EnvirAlert
{
    /// <summary>
    /// Código de vista inicial y principal MainPage   
    /// </summary>
    
    public partial class MainPage : PhoneApplicationPage
    {

        #region DEFINICION DE VARIABLES
        /// Variables de uso para  Coordenadas
        private double la; //(Latitud)
        private double lo; //(Longitud)

        /// Variables de uso para feeds
        private string feedCapturado;

        /// Variables de uso para Alertas
        private Alerta miAlerta;
        private Boolean bandAlerta;



        /// De configuracion

        private int tSegundosAntes = -15;
        private double rangoSeguridad = 0.02;      /// Rango para determinar si hay otros dispositivos que puedan generar alertas
        private string paramConsulta = "universidad del cauca";     ///Consulta que se enviara por parametro al servicio web
        private int tSegundosHayInternet = 4;       /// Tiempo en segundos para intervalos de timpo que se verifica conexión a internet


        /// Auxiliares
        private bool mensajeYaSeMostro = false;

        #endregion

        #region CONSTRUCTORES

        /// <summary>
        /// Constructor, en el cual se inicializan los componentes y se obtiene las coordenadas
        /// del dispositivo en ese momento, y se consulta a el indice semantico para obtener
        /// información de los dispositivos mas cercanos.
        /// </summary>
        public MainPage()
        {

            try
            {
                //Inicialización de componentes
                InitializeComponent();

                la = 0;
                lo = 0;
                feedCapturado = "";
                bandAlerta = false;

                Alerta.vibrar.Stop();
                miAlerta = new Alerta();

                //Obtención de coordenadas
                Coordenadas();

                //Consulta al Indice Semantico, de los dispositivos mas cercanos
                //en intervalos de tiempo constantes.
                ObetenerFeedXively();
            }
            finally
            {

            }

        }

        #endregion

        #region METODOS USADOS
        /// <summary>   
        /// Método Asincrono en el cual se obtiene las coordenadas del GPS
        /// del dispositivo, y se almacenan en las variables miembros
        /// la (latitud) y lo (longitud)
        /// </summary>
        public async void Coordenadas()
        {
            try
            {
                if (HayConexiónInternet())
                {
                    Geolocator myGeolocator = new Geolocator();
                    Geoposition myGeoposition = await myGeolocator.GetGeopositionAsync();
                    //la = Convert.ToDouble(myGeoposition.Coordinate.Latitude);
                    //lo = Convert.ToDouble(myGeoposition.Coordinate.Longitude);
                    la = 2.446479;
                    lo = -76.598496;
                }
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
            dispatcherTimer.Interval = new TimeSpan(0, 0, 30);                      //Se establece intervalos de tiempo (t = 30)
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
              
                    if (ActivarAlerta.IsChecked == true) //Si esta activa la opción de 'Activar Mensajes' en la vista principal en tiempo de ejecución
                    {
                        if (HayConexiónInternet())
                        {
                            WSSemanticSearchSoapClient WS = new WSSemanticSearchSoapClient();
                            WS.RetornarMapaLugarDatapointsAsync(la, lo, paramConsulta, DateTime.Now.AddSeconds(tSegundosAntes), DateTime.Now, "Español", rangoSeguridad);
                            WS.RetornarMapaLugarDatapointsCompleted += ob1_RetornarMapaLugarDatapointsCompleted;
                        }
                    }
                
            }
            finally
            {

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
            //Se toman los resultados de la consulta
            FeedXively[] misFeed = e.Result;

            try
            {

                //Si hay resultados se evalua el dato mas cercano
                if (misFeed.Length > 0)
                {
                    String mensaje = "";
                    feedCapturado = misFeed[0].feed.id.ToString();
                    PagListaSensores tipoAlerta = new PagListaSensores();

                    if (tipoAlerta.riesgo(misFeed[0].feed) == 1)
                    {
                        mensaje = misFeed[0].feed.location.name + "\nEsta área presenta\nuna concentracion de aire\nsegura para respirar";
                    }
                    else if (tipoAlerta.riesgo(misFeed[0].feed) == 2)
                    {
                        mensaje = misFeed[0].feed.location.name + "\nEsta área presenta\n una concentracion de aire\nno tan segura para respirar";
                    }
                    else
                    {
                        mensaje = misFeed[0].feed.location.name + "\nEsta área presenta\nuna concentracion de aire\nPELIGROSA ";
                    }

                    //Otra forma de mostrar mensaje... plan b
                    /*if (MessageBox.Show(mensaje, "Mensaje de alerta", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        NavigationService.Navigate(new Uri("/Views/PagInfoSensores.xaml?feedId=" + misFeed[0].feed.id.ToString(), UriKind.Relative));
                    }*/

                    //Se muestra el mensaje en pantalla de la Alerta que corresponda.
                    if (!Guide.IsVisible)
                    {
                        iniciarSonido();
                        Guide.BeginShowMessageBox("Alerta", mensaje, new string[] { "Detalles", "Omitir" }, 1,
                        MessageBoxIcon.None,
                        new AsyncCallback(OnMessageBoxClosed), null);       //Invocación a OnMessageBoxClosed
                    }
                }
            }
            finally
            {

            }

        }

        /// <summary>
        /// Método asincrono resultante de presionar una de las opciones de la Alerta mostrada
        /// y contiene el resultado de la opción seleccionada, con la cual se podra ver la información
        /// detallada de una alerta, o cancelar la alerta.
        /// </summary>
        /// <param name="ar">Resultado ´de luego de presionar una opción del mensaje de la Alerta</param>
        private void OnMessageBoxClosed(IAsyncResult ar)
        {

            try
            {
                //Se recoge el resultado de la elección del usuario.
                int? buttonIndex = Guide.EndShowMessageBox(ar);

                switch (buttonIndex)
                {
                    case 0:
                        Deployment.Current.Dispatcher.BeginInvoke(() => infoSensor(feedCapturado));
                        break;
                    case 1:
                        Deployment.Current.Dispatcher.BeginInvoke(() => detenerSonido());
                        break;
                    default:
                        break;
                }
            }
            finally
            {

            }
       
        }

        /// <summary>
        /// Se redirecciona de la pagina principal hacia la vista 'PagInfoSensores.xaml' pasando
        /// como parametro el feedId del cual se irá a mostrarmas información de la calidad
        /// del aire.
        /// </summary>
        /// <param name="feedid">Identificador del dispositivo en XIVELY</param>
        public void infoSensor(string feedid)
        {
            try
            {
                detenerSonido();
                NavigationService.Navigate(new Uri("/Views/PagInfoSensores.xaml?feedId=" + feedid, UriKind.Relative));

            }
            finally
            {

            }
            
        }

        /// <summary>
        /// Se inicia el sonido de la alerta.
        /// </summary>
        public void iniciarSonido()
        {
            try
            {
                miAlerta = new Alerta();
                miAlerta.iniciar(mediaControl);
            }
            finally
            {

            }
           
        }

        /// <summary>
        /// Se detiene la alerta.
        /// </summary>
        public void detenerSonido()
        {
            try
            {
                miAlerta.cancelar();
            }
            finally
            {

            }
            
        }

        #endregion

        #region METODOS DE REDIRECCIONAMIENTO A OTRAS VISTAS

        private void irPagListaSensores(object sender, RoutedEventArgs e)
        {   
            NavigationService.Navigate(new Uri("/Views/PagListaSensores.xaml", UriKind.Relative));
        }

        private void irPagVistaGeograficaSensores(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/PagVistaGeograficaSensores.xaml", UriKind.Relative));
        }

        private void irPagTipoBusqueda(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/PagTipoBusqueda.xaml", UriKind.Relative));
        }

        private void hacerIndexacion(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/Indexacion.xaml", UriKind.Relative));
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            try
            {
                miAlerta.cancelar();
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