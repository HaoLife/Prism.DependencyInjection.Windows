using Prism.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Samples.DependencyInjection.ViewModels
{
    public class DatePickerPageViewModel : ViewModelBase
    {
        private readonly INavigationService navService;

        public DatePickerPageViewModel(INavigationService navService)
        {
            this.navService = navService;
            this.BackCommand = new DelegateCommand(() => navService.GoBack());
        }


        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);

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

            this.TestDateTime = DateTime.Now.AddDays(-1);

        }

        public DelegateCommand BackCommand { get; set; }


        private DateTime _testDateTime;

        public DateTime TestDateTime
        {
            get { return _testDateTime; }
            set { SetProperty(ref _testDateTime, value); }
        }



    }
}
