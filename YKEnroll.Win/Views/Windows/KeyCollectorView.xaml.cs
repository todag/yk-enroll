using YKEnroll.Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
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
using Yubico.YubiKey;

namespace YKEnroll.Win.Views.Windows
{    
    /// <summary>
    /// Interaction logic for KeyCollectorWindow.xaml
    /// </summary>
    public partial class KeyCollectorView : Window
    {        
        public KeyCollectorView()
        {
            InitializeComponent();    
        }              
    }
}
