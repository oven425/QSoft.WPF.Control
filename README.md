# QSoft.WPF.Control

## SplitButton (WPF prototype)

`QSoft.WPF.SpiltButtonT` contains a `ContentControl`-based custom control and a
runnable sample. The existing project/namespace spelling is retained.

```xml
<local:SplitButton Content="Save"
                   Command="{Binding SaveCommand}"
                   DropDownBackground="White"
                   DropDownBorderBrush="Gray"
                   DropDownBorderThickness="1"
                   DropDownCornerRadius="12">
    <local:SplitButton.DropDownContent>
        <StackPanel>
            <Button Content="Save as..." Command="{Binding SaveAsCommand}" />
        </StackPanel>
    </local:SplitButton.DropDownContent>
</local:SplitButton>
```

Declare `xmlns:local="clr-namespace:QSoft.WPF.SpiltButtonT"` in the sample project.
The primary button supports `Content`, `ContentTemplate`, `Click`, `Command`,
`CommandParameter`, and `CommandTarget`. WPF's inner button handles command
execution and `CanExecute`; an unavailable primary command does not disable the
drop-down button.

The drop-down accepts arbitrary `DropDownContent` and `DropDownContentTemplate`.
`IsDropDownOpen` supports two-way binding. Outside clicks, Escape, disabling, and
unloading close the popup; F4 or Alt+Down toggles it. Drop-down actions decide
whether to close it (set `IsDropDownOpen` to false).

The default template is in `QSoft.WPF.SpiltButtonT/Themes/Generic.xaml`, with
`PART_PrimaryButton` and `PART_SecondaryButton` (`ButtonBase`) and `PART_Popup`
(`Popup`). Replacement templates should retain the command/content bindings and
the two-way popup `IsOpen` binding and the secondary-button hit-test trigger
(so clicking the arrow dismisses rather than immediately reopens the popup).
The transparent popup hosts a border for
custom background, border, and rounded corners. Place content inside that border's
padding: `CornerRadius` rounds the border, but does not clip overflowing children.
This is the initial WPF architecture, not full WinUI 3 visual/automation parity or
menu-style keyboard navigation.

## RadioButtonList

```xml
<qcontrol:RadioButtonList SelectedValue="{Binding Fix}" SelectedValuePath="Tag" Height="35">
    <i:Interaction.Triggers>
        <i:EventTrigger EventName="SelectionChanged">
            <i:InvokeCommandAction Command="{Binding SetFix1Command}" CommandParameter="{Binding Fix}"/>
        </i:EventTrigger>
    </i:Interaction.Triggers>
    <RadioButton Command="{Binding SetFixCommand}" CommandParameter="{Binding Fix}" GroupName="fix" Tag="5">5</RadioButton>
    <RadioButton Command="{Binding SetFixCommand}" CommandParameter="{Binding Fix}" GroupName="fix" Tag="50">50</RadioButton>
    <qcontrol:RadioButtonList.ItemsPanel>
        <ItemsPanelTemplate>
            <qpanel:FlexPanel JustifyContent="SpaceAround" Gap="10"/>
        </ItemsPanelTemplate>
    </qcontrol:RadioButtonList.ItemsPanel>
</qcontrol:RadioButtonList>

```