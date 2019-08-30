using Prism.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;

namespace Samples.DependencyInjection.ViewModels
{
    public class TabViewPageViewModel : ViewModelBase
    {
        private readonly INavigationService navService;


        public TabViewPageViewModel(INavigationService navService)
        {
            this.navService = navService;
            this.BackCommand = new DelegateCommand(() => navService.GoBack());
        }


        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            this.TestTitle = "begin";

            Console.WriteLine($"thread id {System.Threading.Thread.CurrentThread.ManagedThreadId} - by start from");
            Task.Run(this.Init);
            Console.WriteLine($"thread id {System.Threading.Thread.CurrentThread.ManagedThreadId} - by end from");
        }

        public async Task Init()
        {
            Console.WriteLine($"thread id {System.Threading.Thread.CurrentThread.ManagedThreadId} - by start init yield");
            await Task.Yield();
            Console.WriteLine($"thread id {System.Threading.Thread.CurrentThread.ManagedThreadId} - by end init yield");



            //await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => this.TestTitle = "你好11111。。。。。。。。");

            this.TestTitle = "你好11111。。。。。。。。";

        }

        public DelegateCommand BackCommand { get; set; }


        private string _testTitle;

        public string TestTitle
        {
            get { return _testTitle; }
            set { SetProperty(ref _testTitle, value); }
        }




    }
}
