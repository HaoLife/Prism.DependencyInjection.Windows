using System;
using System.Globalization;
using Windows.UI.Xaml;
using Prism.Logging;
using Prism.Windows;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.Activation;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Prism.Events;
using Prism.Windows.Mvvm;
using Prism.Mvvm;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;

namespace Prism.DependencyInjection.Windows
{
    /// <summary>
    /// Provides the base class for the Windows Store Application object which
    /// includes the automatic creation and wiring of the Autofac container and 
    /// the bootstrapping process for Prism services in the container.
    /// </summary>
    public abstract class PrismDIApplication : Application
    {
        private bool _handledOnResume;
        private bool _isRestoringFromTermination;
        protected UIElement Shell { get; private set; }
        protected Func<SplashScreen, Page> ExtendedSplashScreenFactory { get; set; }
        protected INavigationService NavigationService { get; private set; }
        protected ISessionStateService SessionStateService { get; private set; }
        protected ILoggerFacade Logger { get; set; }
        protected IServiceProvider Provider { get; private set; }
        public bool IsSuspending { get; private set; }
        protected bool RestoreNavigationStateOnResume { get; set; } = true;


        protected PrismDIApplication()
        {
            Suspending += OnSuspending;
            Resuming += OnResuming;

            Logger = CreateLogger();
            if (Logger == null)
            {
                throw new InvalidOperationException("Logger Facade is null");
            }

            Logger.Log("Created Logger", Category.Debug, Priority.Low);
        }


        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            base.OnShareTargetActivated(args);
            OnActivated(args);
        }
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);

            await InitializeShell(args);

            if (Window.Current.Content != null && (!_isRestoringFromTermination || args != null))
            {
                await OnActivateApplicationAsync(args);
            }
            else if (Window.Current.Content != null && _isRestoringFromTermination && !_handledOnResume)
            {
                await OnResumeApplicationAsync(args);
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }
        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            base.OnFileActivated(args);
            OnActivated(args);
        }
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await InitializeShell(args);

            // If the app is launched via the app's primary tile, the args.TileId property
            // will have the same value as the AppUserModelId, which is set in the Package.appxmanifest.
            // See http://go.microsoft.com/fwlink/?LinkID=288842
            string tileId = AppManifestHelper.GetApplicationId();

            if (Window.Current.Content != null && (!_isRestoringFromTermination || (args != null && args.TileId != tileId)))
            {
                await OnLaunchApplicationAsync(args);
            }
            else if (Window.Current.Content != null && _isRestoringFromTermination)
            {
                await OnResumeApplicationAsync(args);
            }

            // Ensure the current window is active
            Window.Current.Activate();
            
        }



        private async Task InitializeShell(IActivatedEventArgs args)
        {
            if (Window.Current.Content == null)
            {
                Frame rootFrame = await InitializeFrameAsync(args);

                Shell = CreateShell(rootFrame);

                Window.Current.Content = Shell ?? rootFrame;
            }
        }

        protected virtual async Task<Frame> InitializeFrameAsync(IActivatedEventArgs args)
        {
            var services = CreateServices();
            var rootFrame = CreateRootFrame(services);
            var eventAggregator = CreateEventAggregator(services);
            var frameFacade = CreateFrameFacade(services, rootFrame, eventAggregator);

            this.SessionStateService = CreateSessionStateService(services);
            this.NavigationService = CreateNavigationService(services, frameFacade, this.SessionStateService);

            services.AddSingleton<Frame>(rootFrame);
            services.AddSingleton<IEventAggregator>(eventAggregator);
            services.AddSingleton<IFrameFacade>(frameFacade);
            services.AddSingleton<ISessionStateService>(this.SessionStateService);
            services.AddSingleton<INavigationService>(this.NavigationService);

            ConfigureServices(services);

            if (ExtendedSplashScreenFactory != null)
            {
                Page extendedSplashScreen = ExtendedSplashScreenFactory.Invoke(args.SplashScreen);
                rootFrame.Content = extendedSplashScreen;
            }

            this.Provider = services.BuildServiceProvider();

            var deviceGestureService = this.Provider.GetRequiredService<IDeviceGestureService>();

            SessionStateAwarePage.GetSessionStateForFrame =
                frame => SessionStateService.GetSessionStateForFrame(frameFacade);

            //Associate the frame with a key
            SessionStateService.RegisterFrame(frameFacade, "AppFrame");

            deviceGestureService.GoBackRequested += OnGoBackRequested;
            deviceGestureService.GoForwardRequested += OnGoForwardRequested;
            ConfigureViewModelLocator();

            OnRegisterKnownTypesForSerialization();

            bool canRestore = await SessionStateService.CanRestoreSessionStateAsync();
            bool shouldRestore = canRestore && ShouldRestoreState(args);
            if (shouldRestore)
            {
                await SessionStateService.RestoreSessionStateAsync();
            }

            Configure(this.Provider);

            await OnInitializeAsync(args);

            if (shouldRestore)
            {
                // Restore the saved session state and navigate to the last page visited
                try
                {
                    SessionStateService.RestoreFrameState();
                    NavigationService.RestoreSavedNavigation();
                    _isRestoringFromTermination = true;
                }
                catch (SessionStateServiceException)
                {
                    // Something went wrong restoring state.
                    // Assume there is no state and continue
                }
            }

            return rootFrame;
        }



        protected virtual Task OnResumeApplicationAsync(IActivatedEventArgs args)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnActivateApplicationAsync(IActivatedEventArgs args) { return Task.CompletedTask; }



        protected virtual IServiceCollection CreateServices()
        {
            return new ServiceCollection();
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDeviceGestureService, DeviceGestureService>();
        }

        protected virtual void Configure(IServiceProvider provider)
        {


        }


        protected virtual Frame CreateRootFrame(IServiceCollection services)
        {
            return new Frame();
        }

        protected virtual UIElement CreateShell(Frame rootFrame) => rootFrame;

        protected virtual IEventAggregator CreateEventAggregator(IServiceCollection services) => new EventAggregator();

        protected virtual IFrameFacade CreateFrameFacade(IServiceCollection services, Frame frame, IEventAggregator eventAggregator)
            => new FrameFacadeAdapter(frame, eventAggregator);

        protected virtual INavigationService CreateNavigationService(IServiceCollection services, IFrameFacade rootFrame, ISessionStateService sessionStateService)
            => new FrameNavigationService(rootFrame, GetPageType, sessionStateService);

        protected virtual ISessionStateService CreateSessionStateService(IServiceCollection services) => new SessionStateService();


        protected virtual void OnGoBackRequested(object sender, DeviceGestureEventArgs e)
        {
            if (!e.Handled)
            {
                if (NavigationService.CanGoBack())
                {
                    NavigationService.GoBack();
                    e.Handled = true;
                }
            }
        }

        protected virtual void OnGoForwardRequested(object sender, DeviceGestureEventArgs e)
        {
            if (NavigationService.CanGoForward())
            {
                NavigationService.GoForward();
                e.Handled = true;
            }
        }

        protected virtual void ConfigureViewModelLocator()
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory(GetService);
        }
        protected virtual object GetService(Type type)
        {
            return ActivatorUtilities.GetServiceOrCreateInstance(this.Provider, type);
        }

        protected virtual void OnRegisterKnownTypesForSerialization() { }

        protected virtual bool ShouldRestoreState(IActivatedEventArgs args) => args.PreviousExecutionState == ApplicationExecutionState.Terminated;

        protected virtual Task OnInitializeAsync(IActivatedEventArgs args) => Task.CompletedTask;

        protected abstract Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args);

        protected virtual Task OnSuspendingApplicationAsync() => Task.CompletedTask;

        protected async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            IsSuspending = true;
            try
            {
                var deferral = e.SuspendingOperation.GetDeferral();

                //Custom calls before suspending.
                await OnSuspendingApplicationAsync();

                //Bootstrap inform navigation service that app is suspending.
                NavigationService.Suspending();

                // Save application state
                await SessionStateService.SaveAsync();

                deferral.Complete();
            }
            finally
            {
                IsSuspending = false;
            }
        }

        protected void OnResuming(object sender, object e)
        {
            if (RestoreNavigationStateOnResume)
                NavigationService.RestoreSavedNavigation();

            _handledOnResume = true;
            OnResumeApplicationAsync(null); // explicit fire and forget, would lock the app if we await
        }


        protected virtual ILoggerFacade CreateLogger()
        {
#if DEBUG        
            return new DebugLogger();
#else
            return new EmptyLogger();
#endif            
        }


        protected virtual Type GetPageType(string pageToken)
        {
            var assemblyQualifiedAppType = GetType().AssemblyQualifiedName;

            var pageNameWithParameter = assemblyQualifiedAppType.Replace(GetType().FullName, GetType().Namespace + ".Views.{0}Page");

            var viewFullName = string.Format(CultureInfo.InvariantCulture, pageNameWithParameter, pageToken);
            var viewType = Type.GetType(viewFullName);

            if (viewType == null)
            {
                var resourceLoader = ResourceLoader.GetForCurrentView("/Prism.Windows/Resources/");
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, resourceLoader.GetString("DefaultPageTypeLookupErrorMessage"), pageToken, GetType().Namespace + ".Views"),
                    nameof(pageToken));
            }

            return viewType;
        }


    }
}
