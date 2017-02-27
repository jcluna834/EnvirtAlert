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
using System.Windows.Shapes;
using System.Windows.Media;
using System.Threading;

using EnvirAlert.WSIOT;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using System.Device.Location;
using System.Windows.Threading;
using Microsoft.Phone.Net.NetworkInformation;
using System.IO;
using System.Windows.Media.Imaging;

namespace EnvirAlert.Views
{
    /// <summary>
    /// Clase de la vista PagLista sensores en la cual se despliega una lista
    /// de dispositivos mas cercanos a determinado rango.
    /// </summary>
    public partial class PagListaSensores : PhoneApplicationPage
    {

        #region DEFINICION DE VARIABLES

        //Listado
        private List<Rectangle> listaRectangulos = new List<Rectangle>();
        private FeedXively[] listaFeeds;

        //Servicio Web 
        private WSIOT.WSSemanticSearchSoapClient WS = new WSSemanticSearchSoapClient();

        //Localización
        private double la; //(latitud)
        private double lo; //(longitud)


        /// De configuracion

        private int tSegundosAntes = -15;
        private double rangoSeguridad = 0.02;      /// Rango para determinar si hay otros dispositivos que puedan generar alertas
        private double rangoConsulta = 1;          /// Rango de dispositivos a consultar
        private string paramConsulta = "universidad del cauca";     ///Consulta que se enviara por parametro al servicio web
        private int tSegundosConsultas = 5;        /// Tiempo en segundos para intervalos de teiempo que se actualiza la información
        private int tSegundosHayInternet = 4;       /// Tiempo en segundos para intervalos de timpo que se verifica conexión a internet



        /// Auxiliares
        private bool mensajeYaSeMostro = false;

        #endregion

        #region CONSTRUCTORES
        /// <summary>
        /// Se inicializan los componentes.
        /// </summary>
        public PagListaSensores()
        {
            la = 0;
            lo = 0;
            InitializeComponent();
            cargarListaSensores();
        }
        #endregion

        #region METODOS USADOS
        
        /// <summary>
        /// Método asincrono el cual se encarga de actualizar la lista de sensores desplegados en la pantalla
        /// cada cierto intervalo de tiempo en las coordenadas indicadas por el GPS de la posición actual.
        /// </summary>
        public async void cargarListaSensores()
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

                    //Se inician las actualizaciones
                    DispatcherTimer dispatcherTimer;
                    dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                    dispatcherTimer.Tick += new EventHandler(cargarDatos_Tick);         //Actualización de datos
                    dispatcherTimer.Interval = new TimeSpan(0, 0, tSegundosConsultas);
                    dispatcherTimer.Start();


                    DispatcherTimer timerConexionInternet;
                    timerConexionInternet = new System.Windows.Threading.DispatcherTimer();
                    timerConexionInternet.Tick += new EventHandler(ConexionInternet_TimerTick);    //Se establece a quien se va a estar invocando
                    timerConexionInternet.Interval = new TimeSpan(0, 0, tSegundosHayInternet);                      //Se establece intervalos de tiempo (t = 4)
                    timerConexionInternet.Start();
                }
                
            }
            finally
            {

            }
            
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
        /// Método invocado desde un dispatcherTimer cada cierto intervalo de tiempo definido.
        /// Desde el indice semantico se obtienen los datos de los dispositivos cercanos a un 
        /// radio de 1 km
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cargarDatos_Tick(object sender, EventArgs e)
        {
            try
            {
                if (HayConexiónInternet())
                {
                    WS.RetornarMapaLugarDatapointsAsync(la, lo, paramConsulta, DateTime.Now.AddSeconds(tSegundosAntes), DateTime.Now, "Español", rangoConsulta);
                    WS.RetornarMapaLugarDatapointsCompleted += c_RetornarMapaLugarDatapointsCompleted;
                }
            }
            finally
            {

            }
           
            
        }


        /// <summary>
        /// Contiene los resultados de la consulta hecha al indice semántico, posteriormente se
        /// pinta en pantalla los cuadros que indican con colores el nivel de riesgo, y un botón
        /// para poder ver en detalle la información del dispositivo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void c_RetornarMapaLugarDatapointsCompleted(object sender, RetornarMapaLugarDatapointsCompletedEventArgs e)
        {
            //Obtención de resultados
            listaFeeds = e.Result;
            try
            {
                int numeroDispositivos = listaFeeds.Length;
                int tamListaDefRow = numeroDispositivos;

                //Borrar la lista de definiciones de filas y columnas, para poder pintar de nuevo cada actualización
                listaDispositivos.RowDefinitions.Clear();
                listaDispositivos.ColumnDefinitions.Clear();
                listaDispositivos.Children.Clear();

                //Definición de columnas
                ColumnDefinition defCol1 = new ColumnDefinition();
                ColumnDefinition defCol2 = new ColumnDefinition();
                defCol1.Width = new GridLength();
                defCol2.Width = new GridLength();

                //Cargando con la configuración básica para los dispositivos
                listaDispositivos.ColumnDefinitions.Add(defCol1);
                listaDispositivos.ColumnDefinitions.Add(defCol2);
                List<RowDefinition> listaDefRow = definicionRows(numeroDispositivos);

                for (int i = 0; i < tamListaDefRow; i++)
                {
                    listaDispositivos.RowDefinitions.Add(listaDefRow[i]);
                }



                //-------------Ya se ha cargado la configuración basica----------------
                //A cargar los dispositivos.. 
                for (int i = 0; i < numeroDispositivos; i++)
                {
                    String tipo = "";
                    //En riesgo(valor) el valor q debe ir ahi es algun valor calculado para decidir si esta en riesgo o no
                    switch (riesgo(listaFeeds[i].feed)) { case 1: tipo = "RectanguloDispositivoBueno"; break; case 2: tipo = "RectanguloDispositivoAceptable"; break; case 3: tipo = "RectanguloDispositivoMalo"; break; }

                    Rectangle rectangulo = new Rectangle();
                    rectangulo.Style = Application.Current.Resources[tipo] as Style;
                    rectangulo.Margin = new Thickness(10);

                    Button b = new Button();
                    b.Content = listaFeeds[i].feed.location.name;
                    b.FontSize = 15;
                    b.Background = new SolidColorBrush(Colors.Black);
                    b.Foreground = new SolidColorBrush(Colors.White);
                    b.BorderBrush = new SolidColorBrush(Colors.Black);
                    b.Width = 400;
                    b.Height = 70;
                    b.Click += b_Click;
                    //b.Margin = new Thickness(2);
                    b.Name = listaFeeds[i].feed.id;
                    b.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    Grid.SetRow(rectangulo, i);
                    Grid.SetColumn(rectangulo, 0);
                    Grid.SetRow(b, i);
                    Grid.SetColumn(b, 1);


                    //Se añaden los dispositivos.. 
                    listaDispositivos.Children.Add(rectangulo);
                    listaDispositivos.Children.Add(b);
                    listaRectangulos.Add(rectangulo);

                }
            }
            finally
            {
                    
            }
            

        }



        /// <summary>
        /// Se pinta en los rectangulos los colores correspondientes a el nivel de risgo
        /// que este representando en una zona por algún dispositivo.
        /// </summary>
        void pintarCuadrosAlertas()
        {
            String tipo = "";
            int tam = listaFeeds.Length;
            try
            {
                for (int i = 0; i < tam; i++)
                {
                    if (HayConexiónInternet())
                    {
                        WS.RetornarDatosSensorAsync(listaFeeds[i].feed.id, DateTime.Now.AddSeconds(tSegundosAntes), DateTime.Now);
                        WS.RetornarDatosSensorCompleted += c_RetornarDatosSensorCompleted;
                    }

                }
            }
            finally
            {

            }
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void c_RetornarDatosSensorCompleted(object sender, RetornarDatosSensorCompletedEventArgs e)
        {

            String tipo = "";
            FeedXively feed = e.Result;
            try
            {
                if (listaRectangulos.Count > 0)
                {

                    Rectangle x = listaRectangulos[listaRectangulos.Count - 1];
                    listaRectangulos.RemoveAt(listaRectangulos.Count - 1);
                    switch (riesgo(feed.feed)) { case 1: tipo = "RectanguloDispositivoBueno"; break; case 2: tipo = "RectanguloDispositivoAceptable"; break; case 3: tipo = "RectanguloDispositivoMalo"; break; }
                    x.Style = Application.Current.Resources[tipo] as Style;
                }
            }
            finally
            {

            }
            
        }


        /// <summary>
        /// Evento que se ejecuta cuando se presiona sobre un botón de los dispositivos 
        /// desplegados en la lista de dispositivos y lo redirecciona ala vista PagInfoSensores
        /// pasando como parametro el feedId para que posteriormente esa vista redenrerice la
        /// información de ese dispositivo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void b_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            NavigationService.Navigate(new Uri("/Views/PagInfoSensores.xaml?feedId=" + b.Name, UriKind.Relative));
        }

        /// <summary>
        /// Evalua el riesgo de una zona con la información de un dispositivo.
        /// </summary>
        /// <param name="feed">Información de un dispositivo</param>
        /// <returns>El nivel de riesgo 1 Bajo, 2 Moderado, 3 Riesgoso</returns>
        public int riesgo(Feed feed)
        {
            double SO2, PM10, O3, NO2, CO, PST;
            int respuesta = 1;
            try
            {
                SO2 = Convert.ToDouble(feed.datastreams[0].current_value.Replace(".", ","));
                NO2 = Convert.ToDouble(feed.datastreams[1].current_value.Replace(".", ","));
                CO = Convert.ToDouble(feed.datastreams[2].current_value.Replace(".", ","));
                O3 = Convert.ToDouble(feed.datastreams[3].current_value.Replace(".", ","));
                PM10 = Convert.ToDouble(feed.datastreams[4].current_value.Replace(".", ","));
                PST = Convert.ToDouble(feed.datastreams[5].current_value.Replace(".", ","));

                if (SO2 > 1000 || NO2 > 800 || CO > 34 || O3 > 700 || PM10 > 400 || PST > 625)
                {
                    respuesta = 3;
                }
                else if (SO2 > 500 || NO2 > 400 || CO > 17 || O3 > 350 || PM10 > 300 || PST > 375)
                {
                    respuesta = 2;
                }
            }
            finally
            {
                
            }
          
            return respuesta;

        }

       


        /// <summary>
        /// Define el numero de filas para poder controlar los elementos añadidos
        /// a la lista.
        /// </summary>
        /// <param name="numeroFilasNecesarias">Equivalen al número de filas que se van a listar</param>
        /// <returns>Lista de definicion de columnas 'RowDefinition'</returns>
        public List<RowDefinition> definicionRows(int numeroFilasNecesarias)
        {
            List<RowDefinition> listaDefRows = new List<RowDefinition>();
            try
            {
                for (int i = 0; i < numeroFilasNecesarias; i++)
                {
                    RowDefinition nuevoRowDefinition = new RowDefinition();
                    GridLength gridLenght = new GridLength();
                    //Aqui poner codigo para que el gridLenght tome el valor de "Auto" en caso 
                    //de que asi como esta por defectono funcione. Es decir q no se vea bien 
                    //el tamaño que se ajusta el dispositivo ese.
                    nuevoRowDefinition.Height = gridLenght;
                    listaDefRows.Add(nuevoRowDefinition);
                }
            }
            finally
            {

            }
           

            return listaDefRows;
        }
        #endregion

        #region OTROS METODOS AUXILIARES

        // Código de ejemplo para compilar una ApplicationBar traducida
        private void BuildLocalizedApplicationBar()
        {
            // Establecer ApplicationBar de la página en una nueva instancia de ApplicationBar.
            ApplicationBar = new ApplicationBar();

            // Crear un nuevo botón y establecer el valor de texto en la cadena traducida de AppResources.
            ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarButtonText;
            ApplicationBar.Buttons.Add(appBarButton);

            // Crear un nuevo elemento de menú con la cadena traducida de AppResources.
            ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
            ApplicationBar.MenuItems.Add(appBarMenuItem);
        }

        private void irInfoSensor(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/PagInfoSensores.xaml", UriKind.Relative));
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

