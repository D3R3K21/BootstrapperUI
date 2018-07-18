using System.Collections.Generic;

namespace BootstrapperUI
{
    public class ConsulServicesResponseModel
    {
        public ConsulNode Node { get; set; }
        public Dictionary<string, ConsulProperties> Services { get; set; }
    }
}
