using Splat;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

using ReactiveUI;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Runtime.CompilerServices;
using ReactiveUIPlay2;

namespace ReactiveUIPlay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            RuntimeHelpers.RunClassConstructor(typeof(RxApp).TypeHandle);
            Locator.CurrentMutable.UnregisterCurrent(typeof(IPropertyBindingHook));
            Locator.CurrentMutable.Register(() => new AutoDataTemplateBindingHook(), typeof(IPropertyBindingHook));
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());
            Locator.CurrentMutable.RegisterViewModelsForModels(Assembly.GetExecutingAssembly());

            return;

            bool done = false;
            Locator.CurrentMutable.ServiceRegistrationCallback(
                typeof(IPropertyBindingHook),
                null,
                callbackHandle =>
                {
                    if (done
                        || Locator.Current.GetService<IPropertyBindingHook>()?.GetType()
                        != typeof(ReactiveUI.AutoDataTemplateBindingHook))
                    {
                        return;
                    }

                    done = true;
                    Locator.CurrentMutable.UnregisterCurrent(typeof(IPropertyBindingHook));
                    Locator.CurrentMutable.Register(() => new AutoDataTemplateBindingHook(), typeof(IPropertyBindingHook));
                    callbackHandle.Dispose();
                });
        }


        public class AutoDataTemplateBindingHook : IPropertyBindingHook
        {
            /// <summary>
            /// Gets the default item template.
            /// </summary>
            [SuppressMessage("Design", "CA1307: Use the currency locale settings", Justification = "Not available on all platforms.")]
            public static Lazy<DataTemplate> DefaultItemTemplate { get; } = new(() =>
            {
#if NETFX_CORE || HAS_UNO
            const string template =
@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:xaml='using:ReactiveUI'>
    <xaml:ViewModelViewHost ViewModel=""{Binding}"" VerticalContentAlignment=""Stretch"" HorizontalContentAlignment=""Stretch"" IsTabStop=""False"" />
</DataTemplate>";
            return (DataTemplate)XamlReader.Load(template);
#else
                const string template = 
                    "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " 
                    + "xmlns:xaml='clr-namespace:ReactiveUIPlay2;assembly=__ASSEMBLYNAME__'> " 
                    + "<xaml:ViewModelViewHost ViewModel=\"{Binding Mode=OneWay}\" VerticalContentAlignment=\"Stretch\" HorizontalContentAlignment=\"Stretch\" IsTabStop=\"False\" />" 
                    + "</DataTemplate>";

                var assemblyName = typeof(AutoDataTemplateBindingHook).Assembly.FullName;
                assemblyName = assemblyName?.Substring(0, assemblyName.IndexOf(','));

#if HAS_WINUI
            return (DataTemplate)XamlReader.Load(template.Replace("__ASSEMBLYNAME__", assemblyName));
#else
                return (DataTemplate)XamlReader.Parse(template.Replace("__ASSEMBLYNAME__", assemblyName));
#endif
#endif
            });

            /// <inheritdoc/>
            public bool ExecuteHook(object? source, object target, Func<IObservedChange<object, object>[]> getCurrentViewModelProperties, Func<IObservedChange<object, object>[]> getCurrentViewProperties, BindingDirection direction)
            {
                if (getCurrentViewProperties is null)
                {
                    throw new ArgumentNullException(nameof(getCurrentViewProperties));
                }

                var viewProperties = getCurrentViewProperties();
                var lastViewProperty = viewProperties.LastOrDefault();

                if (lastViewProperty?.Sender is not ItemsControl itemsControl)
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(itemsControl.DisplayMemberPath))
                {
                    return true;
                }

                if (viewProperties.Last().GetPropertyName() != "ItemsSource")
                {
                    return true;
                }

                if (itemsControl.ItemTemplate is not null)
                {
                    return true;
                }

                if (itemsControl.ItemTemplateSelector is not null)
                {
                    return true;
                }

                itemsControl.ItemTemplate = DefaultItemTemplate.Value;
                return true;
            }
        }

    }
}
