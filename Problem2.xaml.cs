using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Yazlab13
{
    /// <summary>
    /// Interaction logic for Problem2.xaml
    /// </summary>
    public partial class Problem2 : Window
    {
        static int N = 10;
        BackgroundWorker worker;
        int seconds;
        Simulator.ClientAdder adder;

        public Problem2()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_Completed;
            InitializeComponent();

        }

        private Simulator.ClientAdder createConstantAdder()
        {
            int normal = (int)normalClientNum.Value;
            int vip = (int)vipClientNum.Value;
            return (simulator) =>
            {
                if (simulator.steps % seconds == 0)
                {
                    return (normal, vip);
                }
                return (0, 0);
            };
        }

        private void calculateButton_Click(object sender, RoutedEventArgs e)
        {
            seconds = (int)duration.Value;
            bool wantRandom = constantRadioButton.IsChecked == null || randomRadioButton.IsChecked == true;
            adder = wantRandom ? Simulator.RandomFlowClientAdder : createConstantAdder();
            worker.RunWorkerAsync();            
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = Simulator.CalculateMaxProfit(adder, N, seconds, (i) =>
            {
                Dispatcher.Invoke(() => {
                (sender as BackgroundWorker).ReportProgress(i);
                });
            });
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progress.Value = 100 * e.ProgressPercentage / N;
        }

        void worker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            List<SimulationResult> results = (List<SimulationResult>)e.Result;
            dataGrid.DataContext = results;

        }


        private void constantRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if(modelOptionsPanel != null)
            {
                modelOptionsPanel.IsEnabled = true;
            }
        }

        private void randomRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            modelOptionsPanel.IsEnabled = false;
        }
    }
}
