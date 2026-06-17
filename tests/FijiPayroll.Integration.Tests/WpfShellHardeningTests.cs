using System;
using System.ComponentModel;
using System.Threading.Tasks;
using FijiPayroll.WPF.Infrastructure;
using FijiPayroll.WPF.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Integration.Tests;

public sealed class WpfShellHardeningTests
{
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
    public void PriorityDispatcherQueue_ShouldCorrectlyEnqueueAction()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var queue = new PriorityDispatcherQueue(mockLogger);

        // Act
        queue.Enqueue(() => { }, DispatchPriority.Critical);

        // Assert
        queue.PendingCount(DispatchPriority.Critical).Should().Be(1);
        queue.Dispose();
    }

    [Fact]
    public async Task ApplicationStateStore_PropertyChanged_ShouldNotifyFallbackWhenNoApp()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<ApplicationStateStore>>();
        var store = new ApplicationStateStore(mockLogger);
        bool eventRaised = false;

        store.PropertyChanged += (sender, e) =>
        {
            eventRaised = true;
        };

        // Act: Set property from a background thread
        await Task.Run(() =>
        {
            store.SelectedFinancialYear = 2029;
        });

        // Assert: When Application.Current is null, it should fall back to direct notification
        eventRaised.Should().BeTrue();
        store.Dispose();
    }
}
