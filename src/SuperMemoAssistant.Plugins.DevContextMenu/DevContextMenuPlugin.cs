#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// 
// 
// Created On:   7/10/2020 6:45:02 PM
// Modified By:  james

#endregion




namespace SuperMemoAssistant.Plugins.DevContextMenu
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using System.Windows;
  using System.Windows.Input;
  using mshtml;
  using SuperMemoAssistant.Extensions;
  using SuperMemoAssistant.Plugins.DevContextMenu.Interop;
  using SuperMemoAssistant.Plugins.DevContextMenu.UI;
  using SuperMemoAssistant.Services;
  using SuperMemoAssistant.Services.IO.Keyboard;
  using SuperMemoAssistant.Services.Sentry;
  using SuperMemoAssistant.Sys.IO.Devices;
  using SuperMemoAssistant.Sys.Remoting;
  using static SuperMemoAssistant.Plugins.DevContextMenu.HtmlEventEx;


  // Much of this code is adapted from view-source:https://snook.ca/technical/contextmenus/contextmenus.html
  // https://snook.ca/archives/mshtml_and_dec/creating_custom

  // ReSharper disable once UnusedMember.Global
  // ReSharper disable once ClassNeverInstantiated.Global
  [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
  public class DevContextMenuPlugin : SentrySMAPluginBase<DevContextMenuPlugin>
  {
    #region Constructors

    /// <inheritdoc />
    public DevContextMenuPlugin() : base("Enter your Sentry.io api key (strongly recommended)") { }

    #endregion


    #region Properties Impl - Public

    /// <inheritdoc />
    public override string Name => "DevContextMenu";

    /// <inheritdoc />
    public override bool HasSettings => false;
    public HTMLControlEvents events { get; set; }
    public DevContextMenu CurrentWdw { get; set; }
    public DevContextMenuSvc _service { get; set; }

    // Context Menu map from name to action
    public Dictionary<string, ActionProxy> PluginCmdActionMap = new Dictionary<string, ActionProxy>();

    // Turned into HR in Context Menu
    private const string MENU_SEPARATOR = "";

    // Context Item Events
    public HtmlEvent ContextItemClicked = new HtmlEvent();
    public HtmlEvent ContextItemMouseover = new HtmlEvent();
    public HtmlEvent ContextItemMouseout = new HtmlEvent();

    #endregion

    #region Methods Impl

    /// <inheritdoc />
    protected override void PluginInit()
    {

      SubscribeToContextMenuEvent();

      _service = new DevContextMenuSvc();
      PublishService<IDevContextMenu, DevContextMenuSvc>(_service);

    }

    public bool AddMenuItem(string pluginName, string cmd, ActionProxy callback)
    {

      if (pluginName.IsNullOrEmpty() || cmd.IsNullOrEmpty())
        return false;

      PluginCmdActionMap[cmd] = callback;
      return true;

    }

    private void SubscribeToContextMenuEvent()
    {

      var options = new List<EventInitOptions>
      {
        new EventInitOptions(EventType.onmousedown, _ => true, x => ((IHTMLElement)x).tagName.ToLower() == "body"),
      };

      events = new HTMLControlEvents(options);
      events.OnMouseDownEvent += Events_OnMouseDownEvent;

    }

    private void Events_OnMouseDownEvent(object sender, IHTMLControlEventArgs e)
    {

      var eventObj = e?.EventObj;
      if (eventObj.IsNull())
        return;

      if (!PluginCmdActionMap.Any())
        return;

      bool shiftPressed = eventObj.shiftKey;
      bool ctrlPressed = eventObj.ctrlKey;
      bool leftmb = eventObj.button == 1;
      if (!ctrlPressed || !shiftPressed || !leftmb)
        return;

      eventObj.returnValue = false;
      OpenContextMenu(eventObj);

    }

    private void OpenContextMenu(IHTMLEventObj e)
    {

      int x = e.clientX;
      int y = e.clientY;

      var htmlDoc = ContentUtils.GetFocusedHtmlDocument();
      Application.Current.Dispatcher.BeginInvoke((Action)(() =>
      {

        // Create popup
        var window = htmlDoc.parentWindow;
        var popup = ((IHTMLWindow4)window).createPopup() as IHTMLPopup;
        var popupBody = ((IHTMLDocument2)popup.document).body;

        ApplyPopupBodyStyling(popupBody);

        foreach (var cmd in PluginCmdActionMap.Keys)
        {
          AddContextItem(popup, cmd);
        }

        var popupHeight = (PluginCmdActionMap.Count * 17) + 5;
        ContextItemClicked.OnEvent += (sender, args) => popup.Hide();
        popup.Show(x + 2, y + 2, 150, popupHeight, htmlDoc.body);

      }));

    }

    private void AddContextItem(IHTMLPopup popup, string cmdName)
    {
      var doc = popup.document as IHTMLDocument2;
      var body = doc.body as IHTMLDOMNode;
      var el = doc.createElement("<div>");

      ApplyContextItemStyling(el, cmdName);

      if (!cmdName.IsNullOrEmpty())
        SubscribeToContextItemEvents(el);

      body.appendChild((IHTMLDOMNode)el);

      el.style.color = cmdName.IsNullOrEmpty()
        ? "GrayText"
        : "MenuText";

    }

    private void SubscribeToContextItemEvents(IHTMLElement el)
    {
      var element = el as IHTMLElement2;

      // On Context Item Mouseover
      element.SubscribeTo(EventType.onmouseover, ContextItemMouseover);
      ContextItemMouseover.OnEvent += ContextItemMouseover_OnEvent;

      // On Context Item Mouseout
      element.SubscribeTo(EventType.onmouseout, ContextItemMouseout);
      ContextItemMouseout.OnEvent += ContextItemMouseout_OnEvent;

      // On Context Item Click
      element.SubscribeTo(EventType.onclick, ContextItemClicked);
      ContextItemClicked.OnEvent += ContextItemClicked_OnEvent;
      
    }

    private void ContextItemClicked_OnEvent(object sender, IControlHtmlEventArgs e)
    {

      var eventObj = e?.EventObj;
      var contextItem = eventObj?.srcElement;
      if (contextItem.IsNull())
        return;

      // Get innerText and find Action
      string cmd = contextItem.innerText;
      if (!cmd.IsNullOrEmpty() && PluginCmdActionMap.TryGetValue(cmd, out var action))
      {
        action.Invoke();
      }

    }

    private void ContextItemMouseout_OnEvent(object sender, IControlHtmlEventArgs e)
    {
      var src = e.EventObj.srcElement;
      src.style.backgroundColor = "grey";
      src.style.color = "white";
    }

    private void ContextItemMouseover_OnEvent(object sender, IControlHtmlEventArgs e)
    {
      var src = e.EventObj.srcElement;
      src.style.backgroundColor = "white";
      src.style.color = "black";
    }

    private void ApplyContextItemStyling(IHTMLElement el, string text)
    {

      el.style.cursor = "default";
      el.style.width = "100%";
      el.style.textAlign = "center";
      if (text.IsNullOrEmpty())
      {
        el.innerHTML = "<hr>";
        el.style.padding = "2px";
        el.style.margin = "1px";
        el.style.height = "17px";
        el.style.overflow = "hidden";
      }
      else
      {
        el.innerHTML = text;
        el.style.margin = "0px 1px";
        el.style.padding = "2px 20px";
        el.style.fontSize = "10px";
      }

    }

    private void ApplyPopupBodyStyling(IHTMLElement body)
    {
      body.style.border = "solid black 1px";
    }

    #endregion


    #region Methods
    #endregion
  }
}
