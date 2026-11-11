using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace QSoft.WPF.DropDownButtonT
{
    /// <summary>
    /// WPF version of the WinUI 3 DropDownButton: a Button that opens its <see cref="Flyout"/> when clicked.
    /// Flyout can be a ContextMenu (like MenuFlyout), a Popup, or any content (like Flyout).
    /// Style is inherited from Button for now.
    /// </summary>
    public class DropDownButton : Button
    {
        Popup? m_ContentPopup;
        object? m_LogicalFlyout;
        bool m_OwnsPopup;
        bool m_MenuDataContextBound;
        bool m_SuppressClick;

        public static readonly DependencyProperty FlyoutProperty =
            DependencyProperty.Register(nameof(Flyout), typeof(object), typeof(DropDownButton),
                new FrameworkPropertyMetadata(null, (d, e) => ((DropDownButton)d).OnFlyoutChanged(e.OldValue, e.NewValue)));

        public object? Flyout
        {
            get => GetValue(FlyoutProperty);
            set => SetValue(FlyoutProperty, value);
        }

        public static readonly DependencyProperty IsFlyoutOpenProperty =
            DependencyProperty.Register(nameof(IsFlyoutOpen), typeof(bool), typeof(DropDownButton),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (d, e) => ((DropDownButton)d).OnIsFlyoutOpenChanged((bool)e.NewValue)));

        public bool IsFlyoutOpen
        {
            get => (bool)GetValue(IsFlyoutOpenProperty);
            set => SetValue(IsFlyoutOpenProperty, value);
        }

        public static readonly DependencyProperty FlyoutPlacementProperty =
            DependencyProperty.Register(nameof(FlyoutPlacement), typeof(PlacementMode), typeof(DropDownButton),
                new FrameworkPropertyMetadata(PlacementMode.Bottom, (d, e) => ((DropDownButton)d).ApplyPlacement()));

        public PlacementMode FlyoutPlacement
        {
            get => (PlacementMode)GetValue(FlyoutPlacementProperty);
            set => SetValue(FlyoutPlacementProperty, value);
        }

        public static readonly RoutedEvent FlyoutOpenedEvent =
            EventManager.RegisterRoutedEvent(nameof(FlyoutOpened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DropDownButton));

        public event RoutedEventHandler FlyoutOpened
        {
            add => AddHandler(FlyoutOpenedEvent, value);
            remove => RemoveHandler(FlyoutOpenedEvent, value);
        }

        public static readonly RoutedEvent FlyoutClosedEvent =
            EventManager.RegisterRoutedEvent(nameof(FlyoutClosed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DropDownButton));

        public event RoutedEventHandler FlyoutClosed
        {
            add => AddHandler(FlyoutClosedEvent, value);
            remove => RemoveHandler(FlyoutClosedEvent, value);
        }

        public void ShowFlyout() => SetCurrentValue(IsFlyoutOpenProperty, true);

        public void HideFlyout() => SetCurrentValue(IsFlyoutOpenProperty, false);

        protected override void OnClick()
        {
            if (m_SuppressClick || IsFlyoutOpen)
            {
                // Like WinUI light dismiss: a press on the button while the flyout is open only closes it.
                m_SuppressClick = false;
                HideFlyout();
                return;
            }
            base.OnClick();
            ShowFlyout();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled) return;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.F4 || (key == Key.Down && (Keyboard.Modifiers & ModifierKeys.Alt) != 0))
            {
                SetCurrentValue(IsFlyoutOpenProperty, !IsFlyoutOpen);
                e.Handled = true;
            }
        }

        protected override IEnumerator LogicalChildren
        {
            get
            {
                var baseChildren = base.LogicalChildren;
                if (baseChildren != null)
                {
                    while (baseChildren.MoveNext()) yield return baseChildren.Current;
                }
                if (m_LogicalFlyout != null) yield return m_LogicalFlyout;
            }
        }

        void OnFlyoutChanged(object? oldValue, object? newValue)
        {
            SetCurrentValue(IsFlyoutOpenProperty, false);
            DetachFlyout();
            switch (newValue)
            {
                case null:
                    break;
                case Popup popup:
                    m_ContentPopup = popup;
                    HookPopup(popup);
                    SetLogicalFlyout(popup);
                    break;
                default:
                    // No host chrome: the content (e.g. a rounded Border) defines the look; transparency keeps corners clean.
                    var popupHost = new Popup
                    {
                        StaysOpen = false,
                        Focusable = false,
                        AllowsTransparency = true,
                        Child = newValue as UIElement ?? new ContentPresenter { Content = newValue },
                    };
                    m_ContentPopup = popupHost;
                    m_OwnsPopup = true;
                    HookPopup(popupHost);
                    SetLogicalFlyout(popupHost);
                    break;
            }
            ApplyPlacement();
        }

        void DetachFlyout()
        {
            if (m_ContentPopup != null)
            {
                m_ContentPopup.Opened -= Popup_Opened;
                m_ContentPopup.Closed -= Popup_Closed;
                m_ContentPopup.RemoveHandler(PreviewKeyDownEvent, (KeyEventHandler)Popup_PreviewKeyDown);
                if (m_OwnsPopup)
                {
                    m_ContentPopup.Child = null;
                }
                m_ContentPopup = null;
                m_OwnsPopup = false;
            }
            if (m_LogicalFlyout != null)
            {
                RemoveLogicalChild(m_LogicalFlyout);
                m_LogicalFlyout = null;
            }
        }

        void SetLogicalFlyout(FrameworkElement flyout)
        {
            // Only adopt it when nobody else owns it, so DataContext/resources flow from the button.
            if (flyout.Parent == null)
            {
                m_LogicalFlyout = flyout;
                AddLogicalChild(flyout);
            }
        }

        void HookPopup(Popup popup)
        {
            popup.Opened += Popup_Opened;
            popup.Closed += Popup_Closed;
            popup.AddHandler(PreviewKeyDownEvent, (KeyEventHandler)Popup_PreviewKeyDown, true);
        }

        void ApplyPlacement()
        {
            if (m_ContentPopup != null)
            {
                m_ContentPopup.PlacementTarget = this;
                m_ContentPopup.Placement = FlyoutPlacement;
            }
        }

        void OnIsFlyoutOpenChanged(bool isOpen)
        {
            if (isOpen && !IsLoaded)
            {
                // Popups can't be shown before the button is in a visual tree.
                Loaded += OpenWhenLoaded;
                return;
            }
            ApplyPlacement();

            if (m_ContentPopup != null)
            {
                m_ContentPopup.IsOpen = isOpen;
            }
            else if (isOpen)
            {
                SetCurrentValue(IsFlyoutOpenProperty, false);
            }
        }

        void OpenWhenLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OpenWhenLoaded;
            if (IsFlyoutOpen) OnIsFlyoutOpenChanged(true);
        }

        void Menu_Opened(object sender, RoutedEventArgs e) => OnFlyoutOpened();

        void Menu_Closed(object sender, RoutedEventArgs e) => OnFlyoutClosed();

        void Popup_Opened(object? sender, EventArgs e)
        {
            if (m_ContentPopup?.Child is UIElement child)
            {
                child.Dispatcher.BeginInvoke(() =>
                {
                    if (m_ContentPopup?.IsOpen == true)
                    {
                        child.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                    }
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
            OnFlyoutOpened();
        }

        void Popup_Closed(object? sender, EventArgs e)
        {
            if (m_ContentPopup?.Child is UIElement child && child.IsKeyboardFocusWithin)
            {
                Focus();
            }
            OnFlyoutClosed();
        }

        void Popup_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideFlyout();
                Focus();
                e.Handled = true;
            }
        }

        void OnFlyoutOpened()
        {
            SetCurrentValue(IsFlyoutOpenProperty, true);
            RaiseEvent(new RoutedEventArgs(FlyoutOpenedEvent, this));
        }

        void OnFlyoutClosed()
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && IsPointerOverSelf())
            {
                // Light dismiss caused by pressing on this button: swallow the resulting click.
                m_SuppressClick = true;
                InputManager.Current.PostProcessInput += ClearSuppressAfterMouseUp;
            }
            SetCurrentValue(IsFlyoutOpenProperty, false);
            RaiseEvent(new RoutedEventArgs(FlyoutClosedEvent, this));
        }

        bool IsPointerOverSelf()
        {
            if (PresentationSource.FromVisual(this) == null) return false;
            var pt = Mouse.GetPosition(this);
            return pt.X >= 0 && pt.Y >= 0 && pt.X < ActualWidth && pt.Y < ActualHeight;
        }

        void ClearSuppressAfterMouseUp(object sender, ProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is MouseButtonEventArgs args
                && args.RoutedEvent == Mouse.MouseUpEvent
                && args.ChangedButton == MouseButton.Left)
            {
                InputManager.Current.PostProcessInput -= ClearSuppressAfterMouseUp;
                m_SuppressClick = false;
            }
        }
    }
}

