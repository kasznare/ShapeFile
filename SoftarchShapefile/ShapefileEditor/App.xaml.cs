using System.Windows;
#if DEBUG
using System.Diagnostics;
#endif

namespace ShapefileEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
#if DEBUG
        //https://spin.atomicobject.com/2013/12/11/wpf-data-binding-debug/
        //xmlns:diag="clr-namespace:System.Diagnostics;assembly=WindowsBase"
        //diag:PresentationTraceSources.TraceLevel=High
        protected override void OnStartup(StartupEventArgs e)
        {
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DataBindingSource.Listeners.Add(new DebugTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning | SourceLevels.Error;
            base.OnStartup(e);
        }
#endif
    }

#if DEBUG
    public class DebugTraceListener : TraceListener
    {
        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
            //Debugger.Break();
        }
    }
#endif
}
