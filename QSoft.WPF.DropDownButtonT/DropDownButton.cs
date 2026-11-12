using System.Collections;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace QSoft.WPF.DropDownButtonT
{
    /// <summary>
    /// WPF version of the WinUI 3 DropDownButton: a Button that opens its <see cref="Flyout"/> when clicked.
    /// Flyout can be a ContextMenu (like MenuFlyout), a Popup, or any content (like Flyout).
    /// Style is inherited from Button for now.
    /// </summary>
    public class DropDownButton : System.Windows.Controls.Button
    {
        Popup? m_ContentPopup;
        object? m_LogicalFlyout;
        bool m_PressedInPopup;
        bool m_OwnsPopup;
        bool m_SuppressClick;

        static DropDownButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownButton), new FrameworkPropertyMetadata(typeof(DropDownButton)));
        }

        public static readonly DependencyProperty FlyoutProperty =
            DependencyProperty.Register(nameof(Flyout), typeof(object), typeof(DropDownButton),
                new FrameworkPropertyMetadata(null, (d, e) => ((DropDownButton)d).OnFlyoutChanged(e.OldValue, e.NewValue)));

        /// <summary>
        /// The popup content. A completed left click inside the popup closes it by default.
        /// </summary>
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

        public static readonly DependencyProperty CloseFlyoutOnItemClickProperty =
            DependencyProperty.Register(nameof(CloseFlyoutOnItemClick), typeof(bool), typeof(DropDownButton), new FrameworkPropertyMetadata(true));

        /// <summary>Close the flyout on a left click inside it or when a button or menu item is activated.</summary>
        public bool CloseFlyoutOnItemClick
        {
            get => (bool)GetValue(CloseFlyoutOnItemClickProperty);
            set => SetValue(CloseFlyoutOnItemClickProperty, value);
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

        // Input inside the flyout bubbles to this button through the Popup; it must not drive Button press/click logic,
        // otherwise the button captures the mouse, the Popup light-dismisses, and the mouse up reopens it.
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsFromSelf(e.OriginalSource)) base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsFromSelf(e.OriginalSource)) base.OnMouseLeftButtonUp(e);
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (IsFromSelf(e.OriginalSource)) base.OnMouseMove(e);
        }

        protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e)
        {
            if (IsFromSelf(e.OriginalSource)) base.OnKeyUp(e);
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (!IsFromSelf(e.OriginalSource)) return;
            base.OnKeyDown(e);
            if (e.Handled) return;
            var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
            if (key == System.Windows.Input.Key.F4 || (key == System.Windows.Input.Key.Down && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) != 0))
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
                        Child = newValue as UIElement ?? new System.Windows.Controls.ContentPresenter { Content = newValue },
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
                m_ContentPopup.RemoveHandler(PreviewKeyDownEvent, (System.Windows.Input.KeyEventHandler)Popup_PreviewKeyDown);
                m_ContentPopup.RemoveHandler(ClickEvent, (RoutedEventHandler)FlyoutItem_Click);
                m_ContentPopup.RemoveHandler(MouseLeftButtonDownEvent, (System.Windows.Input.MouseButtonEventHandler)Popup_MouseLeftButtonDown);
                m_ContentPopup.RemoveHandler(MouseLeftButtonUpEvent, (System.Windows.Input.MouseButtonEventHandler)Popup_MouseLeftButtonUp);
                m_PressedInPopup = false;
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
            popup.AddHandler(PreviewKeyDownEvent, (System.Windows.Input.KeyEventHandler)Popup_PreviewKeyDown, true);
            popup.AddHandler(ClickEvent, (RoutedEventHandler)FlyoutItem_Click, true);
            popup.AddHandler(MouseLeftButtonDownEvent, (System.Windows.Input.MouseButtonEventHandler)Popup_MouseLeftButtonDown, true);
            popup.AddHandler(MouseLeftButtonUpEvent, (System.Windows.Input.MouseButtonEventHandler)Popup_MouseLeftButtonUp, true);
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

        void Popup_Opened(object? sender, EventArgs e)
        {
            if (m_ContentPopup?.Child is UIElement child)
            {
                child.Dispatcher.BeginInvoke(() =>
                {
                    if (m_ContentPopup?.IsOpen == true)
                    {
                        child.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.First));
                    }
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
            OnFlyoutOpened();
        }

        void Popup_Closed(object? sender, EventArgs e)
        {
            m_PressedInPopup = false;
            if (m_ContentPopup?.Child is UIElement child && child.IsKeyboardFocusWithin)
            {
                Focus();
            }
            OnFlyoutClosed();
        }

        void Popup_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                HideFlyout();
                Focus();
                e.Handled = true;
            }
            else if (!e.Handled && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.None && e.OriginalSource is System.Windows.Controls.Button button && e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down)
            {
                var direction = e.Key == System.Windows.Input.Key.Down ? System.Windows.Input.FocusNavigationDirection.Next : System.Windows.Input.FocusNavigationDirection.Previous;
                e.Handled = button.MoveFocus(new System.Windows.Input.TraversalRequest(direction));
            }
        }

        void OnFlyoutOpened()
        {
            SetCurrentValue(IsFlyoutOpenProperty, true);
            RaiseEvent(new RoutedEventArgs(FlyoutOpenedEvent, this));
        }

        void OnFlyoutClosed()
        {
            if (System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed && IsPointerOverSelf())
            {
                // Light dismiss caused by pressing on this button: swallow the resulting click.
                m_SuppressClick = true;
                System.Windows.Input.InputManager.Current.PostProcessInput += ClearSuppressAfterMouseUp;
            }
            SetCurrentValue(IsFlyoutOpenProperty, false);
            RaiseEvent(new RoutedEventArgs(FlyoutClosedEvent, this));
        }

        void FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsFlyoutOpen && CloseFlyoutOnItemClick) HideFlyout();
        }

        void Popup_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            m_PressedInPopup = IsFlyoutOpen && CloseFlyoutOnItemClick;
        }

        void Popup_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var pressedInPopup = m_PressedInPopup;
            m_PressedInPopup = false;
            if (pressedInPopup && IsFlyoutOpen && CloseFlyoutOnItemClick && m_ContentPopup?.Child is UIElement child)
            {
                var position = e.GetPosition(child);
                if (position.X >= 0 && position.Y >= 0 && position.X < child.RenderSize.Width && position.Y < child.RenderSize.Height)
                    HideFlyout();
            }
        }

        bool IsFromSelf(object? source)
        {
            for (var d = source as DependencyObject; d != null; d = GetParent(d))
            {
                if (ReferenceEquals(d, this)) return true;
            }
            return false;
        }

        // Visual-first walk: flyout content ends at the PopupRoot, so it is never treated as part of this button.
        static DependencyObject? GetParent(DependencyObject d)
            => d is Visual || d is System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);

        bool IsPointerOverSelf()
        {
            if (PresentationSource.FromVisual(this) == null) return false;
            var pt = System.Windows.Input.Mouse.GetPosition(this);
            return pt.X >= 0 && pt.Y >= 0 && pt.X < ActualWidth && pt.Y < ActualHeight;
        }

        void ClearSuppressAfterMouseUp(object sender, System.Windows.Input.ProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is System.Windows.Input.MouseButtonEventArgs args
                && args.RoutedEvent == System.Windows.Input.Mouse.MouseUpEvent
                && args.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                System.Windows.Input.InputManager.Current.PostProcessInput -= ClearSuppressAfterMouseUp;
                m_SuppressClick = false;
            }
        }
    }
}

