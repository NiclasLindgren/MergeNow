﻿using MergeNow.Services;
using MergeNow.Settings;
using MergeNow.ViewModels;
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

        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                ServiceProvider = serviceCollection.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize Merge Now service provider.", ex);
            }

            return base.InitializeAsync(cancellationToken, progress);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<AsyncPackage>(_ => this);
            services.AddSingleton<IMergeNowSettings>(_ => (MergeNowSettings)GetDialogPage(typeof(MergeNowSettings)));
            services.AddSingleton<IMessageService, MessageService>();
            services.AddTransient<IMergeNowService, MergeNowService>();
            services.AddTransient<MergeNowSectionViewModel>();
        }

        public static TControl Resolve<TControl>() where TControl : class
        {
            return ServiceProvider?.GetService<TControl>();
        }
    }
}
