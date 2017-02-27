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
using Microsoft.Phone.Net.NetworkInformation;
using System.Windows.Threading;
using System.IO;
using System.Windows.Media.Imaging;

namespace EnvirAlert.Views
{
    /// <summary>
    /// Vista en la cual se puede añadir un nuevo dispositivo al servicio web
    /// del indice semantico, a travéz del mismo servicio.
    /// </summary>
    public partial class Page1 : PhoneApplicationPage
    {
        #region DEFINICION DE VARIABLES
        private WSSemanticSearchSoapClient WS;


        /// De configuracion

        private int tSegundosHayInternet = 4;       /// Tiempo en segundos para intervalos de timpo que se verifica conexión a internet




        /// Auxiliares
        private bool mensajeYaSeMostro = false;
        #endregion

        #region CONSTRUCTORES
        /// <summary>
        /// Se inicializan Componentes de la vista.
        /// </summary>
        public Page1()
        {
            InitializeComponent();
            try
            {
                if (HayConexiónInternet())
                {
                    WS = new WSSemanticSearchSoapClient();
                }


                DispatcherTimer timerConexionInternet;
                timerConexionInternet = new System.Windows.Threading.DispatcherTimer();
                timerConexionInternet.Tick += new EventHandler(ConexionInternet_TimerTick);    //Se establece a quien se va a estar invocando
                timerConexionInternet.Interval = new TimeSpan(0, 0, tSegundosHayInternet);                      //Se establece intervalos de tiempo (t = 4)
                timerConexionInternet.Start();

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
        #endregion

        #region METODOS USADOS
        /// <summary>
        /// Método asincronico invocado desde un llamado del servicio del indice semantico
        /// CargarFeedXivelyBDD, en el cual se hace posible la inserción de un nuevo dispositivo
        /// existente en XIVELY a este servicio.
        /// </summary>
        /// <param name="sender">Objeto incador</param>
        /// <param name="e">Resultado de la invocación</param>
        void ob_CargarFeedXivelyBDDCompleted(object sender, CargarFeedXivelyBDDCompletedEventArgs e)
        {
            string resultado = e.Result;
            MessageBox.Show(resultado);
        }

        /// <summary>
        /// Válida/Corrige el FeedId
        /// </summary>
        /// <param name="sender">Objeto invocador</param>
        /// <param name="e">Respesta del evento</param>
        private void Verificar(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                CheckOutNumber((TextBox)sender);
            }
            finally
            {

            }

        }

        private void CheckOutNumber(TextBox sender)
        {
            string[] Caracteres = { ",", ".", "-" };

            try
            {
                for (int index = 0; index < Caracteres.Length; index++)
                {
                    ((TextBox)sender).Text = ((TextBox)sender).Text.Replace(Caracteres[index], string.Empty);
                }
                sender.SelectionStart = sender.Text.Length;
            }
            finally
            {

            }

        }

        /// <summary>
        /// Evento cuando se presiona para indexar el dispositivo, y se agrega el nuevo dispositivo
        /// al indice semántico.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnIndexar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (HayConexiónInternet())
                {
                    string feedId = txtFeedID.Text.ToString();
                    WS.CargarFeedXivelyBDDAsync(feedId);
                    WS.CargarFeedXivelyBDDCompleted += ob_CargarFeedXivelyBDDCompleted;
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