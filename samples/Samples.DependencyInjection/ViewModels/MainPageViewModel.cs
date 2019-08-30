using Prism.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Samples.DependencyInjection.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private INavigationService _navigation;
        public MainPageViewModel(INavigationService navService)
        {
            _navigation = navService;

            this.TabViewCommand = new DelegateCommand(() => _navigation.Navigate("TabView", null));
            this.DatePickerCommand = new DelegateCommand(() => _navigation.Navigate("DatePicker", null));
        }


        public DelegateCommand TabViewCommand { get; set; }
        public DelegateCommand DatePickerCommand { get; set; }

    }
}
