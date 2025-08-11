# QSoft.WPF.Control

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