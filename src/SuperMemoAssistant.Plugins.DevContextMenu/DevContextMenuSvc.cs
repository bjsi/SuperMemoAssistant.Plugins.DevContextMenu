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
    public bool AddMenuItem(string name, ActionProxy callback)
    {

      if (name.IsNullOrEmpty())
        return false;

      return Svc<DevContextMenuPlugin>.Plugin.AddMenuItem(name, callback);

    }
  }
}
