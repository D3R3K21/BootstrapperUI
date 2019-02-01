using System;
using System.Collections;
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

        private static Dictionary<string, List<string>> preloadedTemplates = new Dictionary<string, List<string>>();
        //= new Dictionary<string, List<string>>
        //{
        //    { "Custom Integrations", new List<string> { "Campaigns", "Contracts", "Organizations", "Entities", "ValueList" } },
        //    { "SalesForce", new List<string> { "Campaigns", "Contracts", "Organizations", "Entities", "ValueList" } },
        //    { "Rules", new List<string> { "Dedupe", "Contracts", "Organizations", "Entities", "ValueList", "Allocation", "Integrations", } },
        //    { "Synthio", new List<string> { "Contracts", "ValueList" } },
        //    { "LinkedIn", new List<string> { "Contracts", "ValueList", "Users","Schedule", "Campaigns" } },
        //    { "Contracts", new List<string> { "Kickfire", "Rules", "ValueList", "Identity", "Allocation", "Integrations", "Notifications", "Users", "Email", "Campaigns", "Organizations", "Silverpop", "Synthio", "Agreements" } },
        //    { "Entities", new List<string> { "Allocation", "Integrations", "Dedupe", "Contracts", "Performance", "Organizations", "Rules", "Notifications", "Reports", "Payout"} }
        //};

        static MainWindow()
        {
            var servicesTemplate = File.ReadAllText("./services.txt");
            var lines = servicesTemplate.Split(';');
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) || line.Length < 3 || line.Substring(0, 2) == "//") continue;
                var kvp = line.Split(':');
                preloadedTemplates.Add(kvp.First().Trim(), kvp.Last().Split(',').Select(p => p.Trim()).ToList());
            }
        }

        public MainWindow()
        {
            var defaultServices = new List<string> { "Allocation", "Authorization", "Campaigns", "Contracts", "Dedupe", "Email", "Entities", "Forms",
                "HttpRaw", "Identity", "Integrations", "Kickfire", "Marketo", "MarketPlace", "Notifications", "Organizations", "Partners", "Payout", "Performance",
                "Proofs", "Reports", "Rules", "SalesForce", "Silverpop", "Users", "ValueList", "Logs-Integrations", "Synthio", "Tags" }.OrderBy(p => p);


            InitializeComponent();
            services.SelectionMode = SelectionMode.Multiple;
            foreach (var service in defaultServices)
            {
                services.Items.Add(service);
            }

            foreach (var template in preloadedTemplates)
            {
                comboBox.Items.Add(template.Key);
            }
        }

        private void GenerateServiceDefinitions(List<string> selectedServices)
        {
            var uriText = uri.Text;
            var entries = new List<string>();
            Console.Out.WriteLine("Enter Environment Prefix");
            _ep = $"{uriText}.integrate.team";
            var client = new HttpClient($"http://{_ep}:8500/v1/catalog/nodes");
            var getResponse = client.Get<List<ConsulNode>>();
            Node = getResponse.SingleOrDefault()?.Node;
            RefreshServices(Endpoint);
            var notFound = new List<string>();
            foreach (var x in selectedServices)
            {
                var ser = (string)x;
                var serviceDefinition = ConsulServices.FirstOrDefault(p => p.Key.ToLowerInvariant().Contains(ser.ToLowerInvariant()) && p.Key.Contains("50051"));
                if (string.IsNullOrEmpty(serviceDefinition.Key))
                {
                    notFound.Add(ser);
                    continue;
                }
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
            if (notFound.Any())
            {
                MessageBox.Show($"Failed find the following services in consul: {string.Join(", ", notFound)}");
            }
            textBox.Text = string.Join("\n", entries);


            if (envCheckbox.IsChecked.Value)
            {
                string setting = null;
                try
                {
                    setting = $"{new Uri(Endpoint).DnsSafeHost}:8500/v1/kv/win";
                    Environment.SetEnvironmentVariable("CONSUL_SERVER", setting, EnvironmentVariableTarget.Machine);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to set 'CONSUL_SERVER' environment variable to '{setting}'");
                }
            }
            services.SelectedIndex = -1;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedServices = new List<string>();
                foreach (var item in services.SelectedItems)
                {
                    selectedServices.Add((string)item);
                }
                GenerateServiceDefinitions(selectedServices);
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

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = preloadedTemplates[(string)comboBox.SelectedItem];
            GenerateServiceDefinitions(items);
        }
    }
}
