using System;
using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Splat;
using Catel.Logging;
using Catel;
using Catel.Collections;
using System.Reflection;

#if HAS_WINUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#elif NETFX_CORE || HAS_UNO

using System.Windows;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#else
using System.Windows;
using System.Windows.Controls;
using global::ReactiveUI;
using System.Reflection.Metadata;
#endif

#if HAS_UNO

namespace ReactiveUI.Uno
#else

    namespace ReactiveUIPlay2
#endif
    {
        /// <summary>
        /// This content control will automatically load the View associated with
        /// the ViewModel property and display it. This control is very useful
        /// inside a DataTemplate to display the View associated with a ViewModel.
        /// </summary>
        public
#if HAS_UNO
        partial
#endif
        class ViewModelViewHost : TransitioningContentControl, IViewFor, IEnableLogger
        {
            /// <summary>
            /// The default content dependency property.
            /// </summary>
            public static readonly DependencyProperty DefaultContentProperty =
                DependencyProperty.Register(nameof(DefaultContent), typeof(object), typeof(ViewModelViewHost), new PropertyMetadata(null));

            /// <summary>
            /// The view model dependency property.
            /// </summary>
            public static readonly DependencyProperty ViewModelProperty =
                DependencyProperty.Register(nameof(ViewModel), typeof(object), typeof(ViewModelViewHost), new PropertyMetadata(null));

            /// <summary>
            /// The view contract observable dependency property.
            /// </summary>
            public static readonly DependencyProperty ViewContractObservableProperty =
                DependencyProperty.Register(nameof(ViewContractObservable), typeof(IObservable<string>), typeof(ViewModelViewHost), new PropertyMetadata(Observable.Return((string?)null)));

            private string? _viewContract;

            /// <summary>
            /// Initializes a new instance of the <see cref="ViewModelViewHost"/> class.
            /// </summary>
            public ViewModelViewHost()
            {

#if NETFX_CORE
                DefaultStyleKey = typeof(ViewModelViewHost);
#endif
                var platform = Locator.Current.GetService<IPlatformOperations>();
                Func<string?> platformGetter = () => default;

                if (platform is null)
                {
                    // NB: This used to be an error but WPF design mode can't read
                    // good or do other stuff good.
                    this.Log().Error("Couldn't find an IPlatformOperations implementation. Please make sure you have installed the latest version of the ReactiveUI packages for your platform. See https://reactiveui.net/docs/getting-started/installation for guidance.");
                }
                else
                {
                    platformGetter = () => platform.GetOrientation();
                }

                ViewContractObservable = ModeDetector.InUnitTestRunner()
                    ? Observable.Never<string?>()
                    : Observable.FromEvent<SizeChangedEventHandler, string?>(
                      eventHandler =>
                      {
                          void Handler(object? sender, SizeChangedEventArgs e) => eventHandler(platformGetter()!);
                          return Handler;
                      },
                      x => SizeChanged += x,
                      x => SizeChanged -= x)
                      .StartWith(platformGetter())
                      .DistinctUntilChanged();

                var contractChanged = this.WhenAnyObservable(x => x.ViewContractObservable).Do(x => _viewContract = x).StartWith(ViewContract);
                var viewModelChanged = this.WhenAnyValue(x => x.ViewModel).StartWith(ViewModel);
                var vmAndContract = contractChanged
                    .CombineLatest(viewModelChanged, (contract, vm) => (ViewModel: vm, Contract: contract));

                this.WhenActivated(d =>
                {
                    d(contractChanged
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x => _viewContract = x ?? string.Empty));

                    d(vmAndContract.DistinctUntilChanged().Subscribe(x => ResolveViewForViewModel(x.ViewModel, x.Contract)));
                });


                if (CatelEnvironment.IsInDesignMode)
                {
                    return;
                }

                Initialized += ViewModelViewHost_Initialized;
            }
            

            private void ViewModelViewHost_Initialized(object? sender, EventArgs e)
            {
                CreateViewModelGrid();
            }

            /// <summary>
            /// Gets or sets the view contract observable.
            /// </summary>
            public IObservable<string?> ViewContractObservable
            {
                get => (IObservable<string>)GetValue(ViewContractObservableProperty);
                set => SetValue(ViewContractObservableProperty, value);
            }

            /// <summary>
            /// Gets or sets the content displayed by default when no content is set.
            /// </summary>
            public object DefaultContent
            {
                get => GetValue(DefaultContentProperty);
                set => SetValue(DefaultContentProperty, value);
            }

            /// <summary>
            /// Gets or sets the ViewModel to display.
            /// </summary>
            public object? ViewModel
            {
                get => GetValue(ViewModelProperty);
                set => SetValue(ViewModelProperty, value);
            }

            /// <summary>
            /// Gets or sets the view contract.
            /// </summary>
            public string? ViewContract
            {
                get => _viewContract;
                set => ViewContractObservable = Observable.Return(value);
            }

            /// <summary>
            /// Gets or sets the view locator.
            /// </summary>
            public IViewLocator? ViewLocator { get; set; }


            public static string GetUniqueControlName(string controlName)
            {
                var random = Guid.NewGuid().ToString();
                random = random.Replace("-", string.Empty);

                var name = $"{controlName}_{random}";
                return name;
            }

            private void ResolveViewForViewModel(object? viewModel, string? contract)
            {
                if (viewModel is null)
                {
                    Content = DefaultContent;
                    return;
                }

                var viewLocator = ViewLocator ?? ReactiveUI.ViewLocator.Current;
                var viewInstance = viewLocator.ResolveView(viewModel, contract) ?? viewLocator.ResolveView(viewModel);

                if (viewInstance is null)
                {
                    Content = DefaultContent;
                    this.Log().Warn($"The {nameof(ViewModelViewHost)} could not find a valid view for the view model of type {viewModel.GetType()} and value {viewModel}.");
                    return;
                }

                viewInstance.ViewModel = viewModel;

                Content = viewInstance;
            }

            private const string InnerWrapperName = "__catelInnerWrapper";

            private void CreateViewModelGrid(bool force=false)
            {
                var content = Content as FrameworkElement;
                if (!force && content is null)
                {
                    return;
                }

                var viewTypeName = GetType().Name;

                Grid? vmGrid = null;
                if (Content is Grid existingGrid && existingGrid.Name.StartsWith(InnerWrapperName))
                {
                    vmGrid = existingGrid;
                }

                if (vmGrid is null)
                {
                    vmGrid = new Grid
                    {
                        Name = GetUniqueControlName(InnerWrapperName)
                    };

#if false
                if (Enum<WrapOptions>.Flags.IsFlagSet(wrapOptions,
                            WrapOptions.CreateWarningAndErrorValidatorForViewModel))
                    {
                        var warningAndErrorValidator = new WarningAndErrorValidator();
                        warningAndErrorValidator.SetBinding(WarningAndErrorValidator.SourceProperty, new Binding());

                        vmGrid.Children.Add(warningAndErrorValidator);
                    }
#endif

                    Content = null;

                    if (content != null)
                    {
                        vmGrid.Children.Add(content);
                    }

                    Content = vmGrid;
                    //Log.Debug($"Created content wrapper grid for view model for view '{viewTypeName}'");
                }
            }

            private object? ResolveViewModelForModel(object? model, string? contract)
            {
                if (model == null) return null;

                var assembly = Assembly.GetExecutingAssembly();

                var modelType = model.GetType();
                var proposedViewModelType = typeof(IViewFor<>).MakeGenericType(modelType);

                var viewModelType = assembly
                    .DefinedTypes
                    .FirstOrDefault(x => !x.IsAbstract && x.GetInterfaces().Any(y => y == proposedViewModelType));

                if (viewModelType == null) return null;

                //     var typesToRegister = from type in assembly.DefinedTypes.Where(x => !x.IsAbstract)
                //     let @interface = type
                //         .GetInterfaces()
                //         .FirstOrDefault(x => x == proposedViewModelType)
                //     where @interface != null
                //     select type;
                //
                //
                // var viewModelType = Reflection.ReallyFindType(proposedViewModelTypeName, throwOnFailure: false);
                //     if (viewModelType is null) return null;

                var args = model is IEnumerable e
                    ? e.Cast<object>().ToArray()
                    : new[] { model };

                var typeFactory = Catel.IoC.TypeFactory.Default;

                var viewModel = typeFactory.CreateInstanceWithParametersAndAutoCompletionWithTag(
                    viewModelType, 
                    null,
                    args);

                return viewModel;
            }
        }
    }


