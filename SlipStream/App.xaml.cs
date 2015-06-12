using Coligo.Core;
using Coligo.Platform.Container;
using SlipStream.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SlipStream
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DefaultContainer container = new DefaultContainer();

            var sc = SynchronizationContext.Current;
            ColigoEngine.Initialize(container);

            container.AsSingle<MainWindowViewModel>();

        }
    }
}
