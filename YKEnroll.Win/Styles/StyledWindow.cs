using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace YKEnroll.Win.Styles;

      public partial class StyledWindow : ResourceDictionary
      {
      public StyledWindow()
      {
          //InitializeComponent();
      }

      private void Button_Close_Click(object sender, RoutedEventArgs e)
      {
           var window = (Window)((FrameworkElement)sender).TemplatedParent;
           window.Close();
       }

     private void Button_MaximizeRestore_Click(object sender, RoutedEventArgs e)
     {
           var window = (Window)((FrameworkElement)sender).TemplatedParent;
           if (window.WindowState == System.Windows.WindowState.Normal)
                   window.WindowState = System.Windows.WindowState.Maximized;
           else
                   window.WindowState = System.Windows.WindowState.Normal;
       }

    private void Button_Minimize_Click(object sender, RoutedEventArgs e)
     {
           var window = (Window)((FrameworkElement)sender).TemplatedParent;
           window.WindowState = System.Windows.WindowState.Minimized;
       }
 }
