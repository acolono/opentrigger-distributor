using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace com.opentrigger.distributord.Plugins
{
    public interface IPlugin
    {
        void Start(string[] cmdlineArgs);
    }

    public class DummyPlugin : IPlugin
    {
        public void Start(string[] cmdlineArgs)
        {
            if (cmdlineArgs.Contains("--dummy"))
            {
                Console.WriteLine("DummyPlugin");
            }
        }
    }

    public static class Plugins
    {
        public static readonly Dictionary<Type,IPlugin> ActivatedPlugins = new Dictionary<Type, IPlugin>();
        public static void StartPlugins(string[] cmdlineArgs)
        {
            var allClasses = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => t.IsClass);
            var pluginInterface = typeof(IPlugin);
            foreach (var type in allClasses)
            {
                var interfaces = type.GetInterfaces();
                if (interfaces.Contains(pluginInterface))
                {
                    try
                    {
                        var plugin = Activator.CreateInstance(type) as IPlugin;
                        if (plugin != null)
                        {
                            plugin.Start(cmdlineArgs);
                            ActivatedPlugins.Add(type, plugin);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Plugin {type.FullName} Activation failed", ex);
                    }
                }
            }
        }
    }
}
