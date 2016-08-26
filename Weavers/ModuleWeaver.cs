using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Weavers.Service;
using Yodii.Model;

namespace Weavers
{
    public class ModuleWeaver
    {
        public Action<string> LogInfo { get; set; }

        public ModuleWeaver()
        {
            LogInfo = m => { };
        }

        public void Execute()
        {
            var assemblies = GetDependentAssemblies(typeof (IYodiiService).Assembly);

            foreach(var assembly in assemblies)
            {
                var typesToProxy = assembly.DefinedTypes
                    .Where(t => t.IsInterface && t.ImplementedInterfaces
                        .Any(i => i.FullName == typeof (IYodiiService).FullName))
                    .ToList();

                try
                {
                    foreach (var type in typesToProxy.Select(t => t.UnderlyingSystemType))
                    {
                        var proxyDefinition = new DefaultProxyDefinition(type);
                        ProxyFactory.CreateStaticProxy(proxyDefinition);
                    }
                }
                catch (Exception e)
                {
                    LogInfo.Invoke(e.Message);
                }
            }
        }
 
        IEnumerable<Assembly> GetDependentAssemblies(Assembly assembly)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.FullName)
                .Contains(assembly.FullName));
        }
    }
}