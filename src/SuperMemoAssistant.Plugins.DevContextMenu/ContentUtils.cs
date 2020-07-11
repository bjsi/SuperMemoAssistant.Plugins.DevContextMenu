using mshtml;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.DevContextMenu
{
  class ContentUtils
  {
    /// <summary>
    /// Get the IHTMLDocument2 object representing the focused html control of the current element.
    /// </summary>
    /// <returns>IHTMLDocument2 object or null</returns>
    public static IHTMLDocument2 GetFocusedHtmlDocument()
    {

      var ctrlGroup = Svc.SM.UI.ElementWdw.ControlGroup;
      var htmlCtrl = ctrlGroup?.FocusedControl?.AsHtml();
      return htmlCtrl?.GetDocument();

    }


    /// <summary>
    /// Get the IHTMLWindow2 object for the currently focused HtmlControl
    /// </summary>
    /// <returns>IHTMLWindow2 object or null</returns>
    public static IHTMLWindow2 GetFocusedHtmlWindow()
    {

      var ctrlGroup = Svc.SM.UI.ElementWdw.ControlGroup;
      var htmlCtrl = ctrlGroup?.FocusedControl?.AsHtml();
      var htmlDoc = htmlCtrl?.GetDocument();
      if (htmlDoc == null)
        return null;

      return Application.Current.Dispatcher.Invoke(() => htmlDoc.parentWindow );

    }
  }
}
