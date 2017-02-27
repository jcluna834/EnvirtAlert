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
using System.Threading;
using System.ComponentModel;
using System.Windows.Threading;
using Microsoft.Phone.Net.NetworkInformation;
using System.IO;
using System.Windows.Media.Imaging;


namespace EnvirAlert.Views
{
    /// <summary>
    /// Clase de la Vista PagInfoSensores en la cual se despliega
    /// la información mas detallada de un dispositivo en la posición del dispositivo a travéz
    /// del GPS.
    /// </summary>
    public partial class PagInfoSensores : PhoneApplicationPage
    {
        #region DEFINICION DE VARIABLES
        private String feedId;
        private WSSemanticSearchSoapClient WS;

        /// De configuracion

        private int tSegundosAntes = -15;
        private int tSegundosConsultas = 1;        /// Tiempo en segundos para intervalos de teiempo que se actualiza la información
        private int tSegundosHayInternet = 4;       /// Tiempo en segundos para intervalos de timpo que se verifica conexión a internet


        /// Auxiliares
        private bool mensajeYaSeMostro = false;
        #endregion

        #region CONSTRUCTORES
        /// <summary>
        /// Se inicializan Componentes de la vista y a travéz de este
        /// se iniciaN actualizaciones cada intervalo de tiempo definido
        /// </summary>
        public PagInfoSensores()
        {
            InitializeComponent();      //Inicialización de componentes
            actualizar();               //Inicialización de actualizaciones 
        }
        #endregion

        #region METODOS USADOS

        /// <summary>
        /// Método encargado de iniciar actualizaciones para el despliegue de 
        /// el listado de dispositivos cercanos y niveles de contaminación.
        /// </summary>
        public void actualizar()
        {
            try
            {
                if (HayConexiónInternet())
                {
                    WS = new WSSemanticSearchSoapClient();

                    DispatcherTimer dispatcherTimer;
                    dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                    dispatcherTimer.Tick += new EventHandler(actualizaciones_Tick);
                    dispatcherTimer.Interval = new TimeSpan(0, 0, tSegundosConsultas);        //Con t = 1 seg esta funcionando perfecto
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
        /// Este método es invocado desde un DispatcherTimer, por el cual es invocado
        /// cada ciertos intervalos de tiempos definidos y aqui se invoca al Indice
        /// Semantico para recuperar datos de un feedId para recuperar sus datos
        /// de su ultima actualización, y actualizar la lista de dispositivos
        /// en la vista.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void actualizaciones_Tick(object sender, EventArgs e)
        {
            if (HayConexiónInternet())
            {
                WS.RetornarDatosSensorAsync(feedId, DateTime.Now.AddSeconds(tSegundosAntes), DateTime.Now);
                WS.RetornarDatosSensorCompleted += wsCliente_RetornarDatosSensorCompleted;
            }

        }

        /// <summary>
        /// Este método es el resultado de una consulta al indice semantico en el cual
        /// se recuperan los ultimos registros agregados de un dispositivo (feedId),
        /// y se actualiza su color y nivel de Contaminación.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void wsCliente_RetornarDatosSensorCompleted(object sender, RetornarDatosSensorCompletedEventArgs e)
        {
            String tipo = "";
            FeedXively feed = e.Result;

            try
            {
                txtDispositivo.Text = feed.feed.title + "\nEn: " + feed.feed.location.name;
                txtSO2.Text = feed.feed.datastreams[0].current_value;
                txtNO2.Text = feed.feed.datastreams[1].current_value;
                txtCO.Text = feed.feed.datastreams[2].current_value;
                txtO3.Text = feed.feed.datastreams[3].current_value;
                txtPM10.Text = feed.feed.datastreams[4].current_value;
                txtPST.Text = feed.feed.datastreams[5].current_value;

                switch (riesgo(feed))
                {
                    case 1:
                        tipo = "RectanguloDispositivoBueno";
                        break;
                    case 2:
                        tipo = "RectanguloDispositivoAceptable";
                        break;
                    case 3: tipo = "RectanguloDispositivoMalo";
                        break;
                }

                rtgEstado.Style = Application.Current.Resources[tipo] as Style;

                r1.Style = Application.Current.Resources[riesgo("SO2", feed)] as Style;
                r2.Style = Application.Current.Resources[riesgo("NO2", feed)] as Style;
                r3.Style = Application.Current.Resources[riesgo("CO", feed)] as Style;
                r4.Style = Application.Current.Resources[riesgo("O3", feed)] as Style;
                r5.Style = Application.Current.Resources[riesgo("PM10", feed)] as Style;
                r6.Style = Application.Current.Resources[riesgo("PST", feed)] as Style;

            }
            finally
            {

            }

        }

        /// <summary>
        /// Se encarga de evaluar el nivel de contaminación de un feedID
        /// Y asignar un estilo añ rectangulo principal de para indicar
        /// el nivel de contaminación en una zona.
        /// </summary>
        /// <param name="feed">Información de un dispositivo</param>
        /// <returns>Nivel de riesgo: 1 Bajo, 2 Moderado, 3 Riesgoso</returns>
        public int riesgo(FeedXively feed)
        {
            double SO2, PM10, O3, NO2, CO, PST;
            int respuesta = 1;

            try
            {

                SO2 = Convert.ToDouble(feed.feed.datastreams[0].current_value.Replace(".", ","));
                NO2 = Convert.ToDouble(feed.feed.datastreams[1].current_value.Replace(".", ","));
                CO = Convert.ToDouble(feed.feed.datastreams[2].current_value.Replace(".", ","));
                O3 = Convert.ToDouble(feed.feed.datastreams[3].current_value.Replace(".", ","));
                PM10 = Convert.ToDouble(feed.feed.datastreams[4].current_value.Replace(".", ","));
                PST = Convert.ToDouble(feed.feed.datastreams[5].current_value.Replace(".", ","));



                //********************************/
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
        /// Evalua el riesgo de uno de los componentes del dispositivo, y 
        /// se pinta de un color, indicando el nivel de riesgo de esa 
        /// zona.
        /// </summary>
        /// <param name="componente">Componente del dispositivo</param>
        /// <param name="feed">Información de todo un dispositivo</param>
        /// <returns></returns>
        public String riesgo(String componente, FeedXively feed)
        {


            String resultado = "RectanguloDispositivoBueno";
            Double SO2 = Convert.ToDouble(feed.feed.datastreams[0].current_value.Replace(".", ","));
            Double NO2 = Convert.ToDouble(feed.feed.datastreams[1].current_value.Replace(".", ","));
            Double CO = Convert.ToDouble(feed.feed.datastreams[2].current_value.Replace(".", ","));
            Double O3 = Convert.ToDouble(feed.feed.datastreams[3].current_value.Replace(".", ","));
            Double PM10 = Convert.ToDouble(feed.feed.datastreams[4].current_value.Replace(".", ","));
            Double PST = Convert.ToDouble(feed.feed.datastreams[5].current_value.Replace(".", ","));
            switch (componente)
            {
                case "SO2":
                    if (SO2 > 1000)
                    {
                        resultado = "RectanguloDispositivoMalo";
                    }
                    else
                    {
                        if (SO2 > 500)
                        {
                            resultado = "RectanguloDispositivoAceptable";
                        }
                    }
                    break;
                case "NO2":
                    if (NO2 > 800)
                    {
                        resultado = "RectanguloDispositivoMalo";
                    }
                    else
                    {
                        if (NO2 > 400)
                        {
                            resultado = "RectanguloDispositivoAceptable";
                        }
                    }
                    break;
                case "CO":
                    if (CO > 34)
                    {
                        resultado = "RectanguloDispositivoMalo";
                    }
                    else
                    {
                        if (CO > 17)
                        {
                            resultado = "RectanguloDispositivoAceptable";
                        }
                    }
                    break;
                case "O3":
                    if (O3 > 700)
                    {
                        resultado = "RectanguloDispositivoMalo";
                    }
                    else
                    {
                        if (O3 > 350)
                        {
                            resultado = "RectanguloDispositivoAceptable";
                        }
                    }
                    break;
                case "PM10":
                    if (PM10 > 400)
                    {
                        resultado = "RectanguloDispositivoMalo";
                    }
                    else
                    {
                        if (PM10 > 300)
                        {
                            resultado = "RectanguloDispositivoAceptable";
                        }
                    }
                    break;
                case "PST":

                    if (PST > 625)
                    {
                        resultado = "RectanguloDispositivoMalo";
                    }
                    else
                    {
                        if (PST > 375)
                        {
                            resultado = "RectanguloDispositivoAceptable";
                        }
                    }
                    break;
            }

            return resultado;
        }


        #endregion

        #region METODOS/EVENTOS DE REDIRECCIONAMIENTO A OTRAS VISTAS
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {
                if (NavigationContext.QueryString.TryGetValue("feedId", out feedId))
                {
                    txtDispositivo.Text = feedId;
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