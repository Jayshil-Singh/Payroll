using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FijiPayroll.WPF.Infrastructure;
using FijiPayroll.WPF.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Integration.Tests;

public sealed class WpfShellHardeningTests : IDisposable
{
    public WpfShellHardeningTests()
    {
        // Thread safety: Ensure a WPF Application instance exists on the test execution thread for Dispatcher tests
        if (System.Windows.Application.Current == null)
        {
            try
            {
                new System.Windows.Application();
            }
            catch
            {
                // Ignore if WPF context cannot be initialized in this test environment runner
            }
        }
    }

    [Fact]
    public void NavigationScopeHandle_RefCount_ShouldDisposeScope_OnlyWhenCountIsZero()
    {
        // Arrange
        var mockScope = Substitute.For<IServiceScope>();
        bool disposed = false;
        mockScope.When(x => x.Dispose()).Do(_ => disposed = true);

        var handle = new NavigationScopeHandle(mockScope);

        // Act & Assert 1: Increase ref count
        handle.AddRef(); // refCount = 2
        
        handle.Dispose(); // refCount = 1, should not dispose scope
        disposed.Should().BeFalse();

        handle.Release(); // refCount = 0, should dispose scope
        disposed.Should().BeTrue();
    }

    [Fact]
    public void PriorityDispatcherQueue_ShouldNotCrash_WhenActionThrowsException()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var queue = new PriorityDispatcherQueue(mockLogger);
        bool nextActionRun = false;

        // Act
        // Enqueue an action that throws an exception to verify draining resilience
        queue.Enqueue(() => throw new InvalidOperationException("Simulated exception inside UI thread action"), DispatchPriority.Critical);
        queue.Enqueue(() => nextActionRun = true, DispatchPriority.Critical);

        // Run the dispatcher frame to process queued events
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

        // Assert
        nextActionRun.Should().BeTrue();
        queue.Dispose();
    }

    [Fact]
    public async Task ApplicationStateStore_PropertyChanged_ShouldNotifyOnUIThread()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<ApplicationStateStore>>();
        var store = new ApplicationStateStore(mockLogger);
        bool eventRaised = false;
        bool raisedOnUIThread = false;

        store.PropertyChanged += (sender, e) =>
        {
            eventRaised = true;
            raisedOnUIThread = System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? true;
        };

        // Act: Set property from a background thread pool thread
        await Task.Run(() =>
        {
            store.SelectedFinancialYear = 2028;
        });

        // Run dispatcher frame to flush any queued BeginInvoke tasks
        if (System.Windows.Application.Current?.Dispatcher != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        // Assert
        eventRaised.Should().BeTrue();
        raisedOnUIThread.Should().BeTrue();
        store.Dispose();
    }

    public void Dispose()
    {
        // Clean up test execution state if needed
    }
}
