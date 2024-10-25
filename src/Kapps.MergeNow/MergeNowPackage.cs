using MergeNow.Services;
using MergeNow.Settings;
using MergeNow.ViewModels;
using MergeNow.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace MergeNow
{
    [ProvideOptionPage(typeof(MergeNowSettings), "Merge Now", "General", 0, 0, true)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuidString)]
    public sealed class MergeNowPackage : AsyncPackage
    {
        public const string PackageGuidString = "0741861b-9c7e-4aad-a1d2-04379f0caa44";

        private static IServiceProvider ServiceProvider { get; set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMergeNowSettings>(_ => (MergeNowSettings)GetDialogPage(typeof(MergeNowSettings)));
            services.AddTransient<IMergeNowService>(sp =>
            {
                var settings = sp.GetService<IMergeNowSettings>();
                return new MergeNowService(this, settings);
            });
            services.AddTransient<MergeNowSectionViewModel>();
            services.AddTransient(sp => new MergeNowSectionControl
            {
                DataContext = sp.GetService<MergeNowSectionViewModel>()
            });
        }

        public static TControl Resolve<TControl>() => ServiceProvider.GetService<TControl>();
    }
}
