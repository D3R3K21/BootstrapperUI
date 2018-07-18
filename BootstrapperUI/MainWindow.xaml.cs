﻿using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BootstrapperUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Dictionary<string, string> MappedServiceNames = new Dictionary<string, string>
        {
            { "Logs-Integrations", "IntegrationsLog"}
        };
        public static Dictionary<string, ConsulProperties> ConsulServices;
        private static string Node { get; set; }
        private static string _ep;
        private static string Endpoint => $"http://{_ep}:8500/v1/catalog/node/{Node}";

        public MainWindow()
        {
            var defaultServices = new List<string> { "Allocation", "Authorization", "Campaigns", "Contracts", "Dedupe", "Email", "Entites", "Forms",
                "HttpRaw", "Integrations", "Kickfire", "Marketo", "MarketPlace", "Notifications", "Organizations", "Partners", "Payout", "Performance",
                "Proofs", "Reports", "Rules", "SalesForce", "Silverpop", "Users", "ValueLists", "Logs-Integrations", "Synthio", "Tags" }.OrderBy(p => p);

            InitializeComponent();
            services.SelectionMode = SelectionMode.Multiple;
            foreach (var service in defaultServices)
            {
                services.Items.Add(service);
            }

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(filePath.Text))
                {
                    MessageBox.Show(this, "File Already Exists");
                }
                var directoryName = System.IO.Path.GetDirectoryName(filePath.Text);
                if (!Directory.Exists(directoryName))
                {
                    MessageBox.Show(this, $"Path Does Not Exist : {directoryName}");
                }
                var uriText = uri.Text;
                var selectedServices = services.SelectedItems;
                var entries = new List<string>();
                Console.Out.WriteLine("Enter Environment Prefix");
                _ep = $"release-{uriText}.integrate.team";
                var client = new HttpClient($"http://{_ep}:8500/v1/catalog/nodes");
                var getResponse = client.Get<List<ConsulNode>>();
                Node = getResponse.SingleOrDefault()?.Node;
                RefreshServices(Endpoint);
                foreach (var x in selectedServices)
                {
                    var ser = (string)x;
                    var serviceDefinition = ConsulServices.FirstOrDefault(p => p.Key.ToLowerInvariant().Contains(ser.ToLowerInvariant()) && p.Key.Contains("50051"));
                    string selectedService;
                    if (!MappedServiceNames.TryGetValue(ser, out selectedService))
                    {
                        selectedService = ser;
                    }

                    var entry = "ServiceFactory.Define(() => \n{\n\t" +
                                $"var channel = new Channel(\"{_ep}\", {serviceDefinition.Value.Port}, ChannelCredentials.Insecure);\n\t" +
                                $"return new ServiceDefinition<{selectedService}Service.{selectedService}ServiceClient>( new {selectedService}Service.{selectedService}ServiceClient(channel), channel); \n}});";
                    entries.Add(entry);
                }

                using (var stream = File.Create(filePath.Text))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        entries.ForEach(p => writer.WriteLine(p + "\n"));
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        static void RefreshServices(string endpoint)
        {
            var client = new HttpClient(endpoint);
            var s = client.Get<ConsulServicesResponseModel>();
            ConsulServices = new Dictionary<string, ConsulProperties>();
            if (s?.Services == null)
            {
                Console.Out.WriteLine($"Endpoint not found : {endpoint}");
                return;
            }
            foreach (var g in s.Services)
            {
                var key = g.Value.Service;
                if (!ConsulServices.ContainsKey(key))
                    ConsulServices.Add(key, g.Value);
            }
            Console.Out.WriteLine($"{ConsulServices.Count} Services Loaded From : {endpoint}");
        }
    }
}
