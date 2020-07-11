using PluginManager.Interop.Sys;
using SuperMemoAssistant.Plugins.DevContextMenu.Interop;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Sys.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.DevContextMenu
{
  public class DevContextMenuSvc : PerpetualMarshalByRefObject, IDevContextMenu
  {
    public bool AddMenuItem(string pluginName, string cmd, ActionProxy callback)
    {

      if (pluginName.IsNullOrEmpty() || cmd.IsNullOrEmpty())
        return false;

      return Svc<DevContextMenuPlugin>.Plugin.AddMenuItem(pluginName, cmd, callback);

    }
  }
}
