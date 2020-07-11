using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace SuperMemoAssistant.Plugins.DevContextMenu.UI
{
  /// <summary>
  /// Interaction logic for ContextMenu.xaml
  /// </summary>
  public partial class DevContextMenu : Window
  {
    public bool IsClosed { get; set; } = false;

    public DevContextMenu(int x, int y)
    {
      InitializeComponent();
      this.Left = x;
      this.Top = y;
      Closed += DevContextMenu_Closed;
    }

    private void DevContextMenu_Closed(object sender, EventArgs e)
    {
      IsClosed = true;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      // this.Deactivated += (sender, args) => Close();
      // DummyButton.ContextMenu.IsOpen = true;
      DummyButton.ContextMenu.Closed += (sender, args) => Close();
    }

    private void ContextMenu1_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        Close();
      }
    }
  }
}
