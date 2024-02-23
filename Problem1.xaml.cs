using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
    /// Interaction logic for Problem1.xaml
    /// </summary>
    public partial class Problem1 : Window
    {
        Simulator simulator;
        public Problem1()
        {
            InitializeComponent();

        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            simulator = new Simulator((int)tables.Value, (int)waiters.Value, (int)cooks.Value, (int)cashiers.Value, Simulator.RandomFlowClientAdder);
            nextButton.IsEnabled = true;
            onNext();
        }

        private void onNext()
        {
            simulator.Step();
            dataGrid.DataContext = new List<Client>(simulator.processedClients);
            
            step.Content = simulator.steps.ToString();
            clients.Content = simulator.totalClients.ToString();
        }
        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            onNext();
        }
    }
}
