using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using YKEnroll.Win.Styles;

namespace YKEnroll.Win.Views.Windows
{   
    public partial class MainView : Window
    {
        public MainView()
        {
            this.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }        
    }
}
