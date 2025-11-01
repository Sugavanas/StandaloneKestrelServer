using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace TS.StandaloneKestrelServer
{
    public class StandaloneKestrelServerConfigurationLoader
    {
        public bool ReloadOnChange { get; }
            
        protected IConfiguration Configuration { get; }
        
        public StandaloneKestrelServerConfigurationLoader(IConfiguration configuration, bool reloadOnChange)
        {
            Configuration = configuration;
            ReloadOnChange = reloadOnChange;
        }

        public string GetServerType()
        {
            return Configuration.GetValue<string>("ServerType");
        }
        
        public string GetApplicationType()
        {
            return Configuration.GetValue<string>("ApplicationType");
        }

        public IChangeToken GetReloadToken()
        {
            return Configuration.GetReloadToken();
        }
    }
}