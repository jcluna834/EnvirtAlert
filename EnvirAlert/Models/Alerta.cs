using Microsoft.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace EnvirAlert.Models
{
    class Alerta
    {
        public BackgroundWorker bw = new BackgroundWorker();
        public String cadena;
        public Boolean bandDeteniendo = false;
        public int numAleatorio = 0;
        public MediaElement sonido;
        public static VibrateController vibrar = VibrateController.Default;

        public Alerta(){           

            sonido = new MediaElement();
            sonido.Source = new Uri("/Media/Alerta1.mp3",UriKind.Relative);
            if (sonido.AutoPlay)
            {
                sonido.Play();
            }
            sonido.Play();

            //vibrar = VibrateController.Default;

            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
        }

        public void iniciar(MediaElement a)
        {
            sonido = a;
            if (bw.IsBusy != true)
            {                
                bw.RunWorkerAsync();                
            }
        }

        public void cancelar()
        {
            if (sonido != null)
                sonido.Stop();
            vibrar.Stop();
            bandDeteniendo = true;

            if (bw.WorkerSupportsCancellation == true)
            {
                bw.CancelAsync();              
                
            }
            else
            {
               
            }            
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            Boolean band = false;
            while (e.Cancel == false)
            {
                if ((worker.CancellationPending == true))
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    if (band != false)
                    {
                        System.Threading.Thread.Sleep(6000);
                    }
                    else
                    {
                        band = true;
                    }
                    worker.ReportProgress(0);
                }
            }
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                cadena = "Canceled!";                
            }

            else if (!(e.Error == null))
            {
                cadena = ("Error: " + e.Error.Message);
            }
            else
            {
                cadena = "Done!";
            }          
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (bandDeteniendo == false)
            {
                //if (sonido != null)
                sonido.Play();
                vibrar.Start(TimeSpan.FromSeconds(3));
            }
        }
    }
}
