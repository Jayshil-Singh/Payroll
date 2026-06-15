using FijiPayroll.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace FijiPayroll.WPF.Helpers;

/// <summary>
/// Defines the restriction actions to apply when a user lacks UI permissions.
/// </summary>
public enum SecurityAction
{
    /// <summary>
    /// Hide the visual control entirely (Visibility = Collapsed).
    /// </summary>
    Hide,

    /// <summary>
    /// Disable interaction with the control (IsEnabled = False).
    /// </summary>
    Disable
}

/// <summary>
/// WPF attached dependency properties for UI-level permission validation.
/// </summary>
public static class Security
{
    /// <summary>
    /// Attached property to specify the required permission code for a UI element.
    /// </summary>
    public static readonly DependencyProperty RequiredPermissionProperty =
        DependencyProperty.RegisterAttached(
            "RequiredPermission",
            typeof(string),
            typeof(Security),
            new PropertyMetadata(null, OnRequiredPermissionChanged));

    public static string GetRequiredPermission(DependencyObject obj)
    {
        return (string)obj.GetValue(RequiredPermissionProperty);
    }

    public static void SetRequiredPermission(DependencyObject obj, string value)
    {
        obj.SetValue(RequiredPermissionProperty, value);
    }

    /// <summary>
    /// Attached property to specify the restriction action (Hide/Disable) when permission fails.
    /// </summary>
    public static readonly DependencyProperty RestrictionActionProperty =
        DependencyProperty.RegisterAttached(
            "RestrictionAction",
            typeof(SecurityAction),
            typeof(Security),
            new PropertyMetadata(SecurityAction.Hide, OnRestrictionActionChanged));

    public static SecurityAction GetRestrictionAction(DependencyObject obj)
    {
        return (SecurityAction)obj.GetValue(RestrictionActionProperty);
    }

    public static void SetRestrictionAction(DependencyObject obj, SecurityAction value)
    {
        obj.SetValue(RestrictionActionProperty, value);
    }

    private static void OnRequiredPermissionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ApplySecurity(d);
    }

    private static void OnRestrictionActionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ApplySecurity(d);
    }

    /// <summary>
    /// Evaluates the claim checks of ICurrentUserService on the given UI element.
    /// </summary>
    public static void ApplySecurity(DependencyObject d)
    {
        if (d is not UIElement element) return;

        string permission = GetRequiredPermission(d);
        if (string.IsNullOrEmpty(permission))
        {
            return; // No permission restrictions specified
        }

        try
        {
            // Resolve ICurrentUserService from Application.Current ServiceProvider
            if (System.Windows.Application.Current is App app && app.ServiceProvider != null)
            {
                var currentUserService = app.ServiceProvider.GetService<ICurrentUserService>();

                if (currentUserService != null)
                {
                    bool hasPermission = currentUserService.HasPermission(permission);
                    if (!hasPermission)
                    {
                        SecurityAction action = GetRestrictionAction(d);
                        if (action == SecurityAction.Hide)
                        {
                            element.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            element.IsEnabled = false;
                        }
                    }
                    else
                    {
                        element.Visibility = Visibility.Visible;
                        element.IsEnabled = true;
                    }
                    return;
                }
            }

            // Secure by default fallback if DI/services are not configured
            element.Visibility = Visibility.Collapsed;
        }
        catch
        {
            // Do NOT throw exceptions during UI layout updates; fail secure
            element.Visibility = Visibility.Collapsed;
        }
    }
}
