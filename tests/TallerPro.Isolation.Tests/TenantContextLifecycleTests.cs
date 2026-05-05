using Shouldly;
using TallerPro.Security;
using Xunit;

namespace TallerPro.Isolation.Tests;

/// <summary>
/// T-27 / R-2: Tests AsyncLocal lifecycle and isolation guarantees of AmbientTenantContext.
/// Verifies that consecutive requests don't contaminate each other and that parallel tasks maintain isolation.
/// These tests do NOT require database — they test the context isolation directly.
/// </summary>
public sealed class TenantContextLifecycleTests
{
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public void SequentialRequests_FirstSetsAndClears_SecondWithoutSet_ThrowsOnRead()
    {
        // Simulate two sequential "requests" in the same execution context.
        // Request 1: set tenant, use it, clear it.
        var context = new AmbientTenantContext();
        context.TrySetTenant(123);
        var firstTenantId = context.CurrentTenantId;
        firstTenantId.ShouldBe(123);
        context.Clear();

        // Request 2: do NOT set tenant, attempt to read.
        var ex = Should.Throw<MissingTenantContextException>(
            () => { _ = context.CurrentTenantId; });
        ex.ShouldNotBeNull();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public void SequentialRequests_ReverseOrder_SecondStillThrowsIfNotSet()
    {
        var context = new AmbientTenantContext();

        // Request 1: attempt to read without setting
        var ex1 = Should.Throw<MissingTenantContextException>(
            () => { _ = context.CurrentTenantId; });
        ex1.ShouldNotBeNull();

        // Request 2: set tenant, verify it works
        context.TrySetTenant(456);
        var secondTenantId = context.CurrentTenantId;
        secondTenantId.ShouldBe(456);

        // Request 3: clear and verify next read fails
        context.Clear();
        var ex2 = Should.Throw<MissingTenantContextException>(
            () => { _ = context.CurrentTenantId; });
        ex2.ShouldNotBeNull();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public async Task ParallelRequests_EachTaskHasIsolatedTenant()
    {
        var context = new AmbientTenantContext();

        // Task 1: set tenant 111
        var task1Result = Task.Run(() =>
        {
            context.TrySetTenant(111);
            var tenantId = context.CurrentTenantId;
            return tenantId;
        });

        // Task 2: set tenant 222
        var task2Result = Task.Run(() =>
        {
            context.TrySetTenant(222);
            var tenantId = context.CurrentTenantId;
            return tenantId;
        });

        var result1 = await task1Result;
        var result2 = await task2Result;

        // Each task should have seen its own tenant (AsyncLocal isolation)
        result1.ShouldBe(111);
        result2.ShouldBe(222);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public void ClearIdempotent_MultipleClears_DoNotThrow()
    {
        var context = new AmbientTenantContext();
        context.TrySetTenant(789);

        // Multiple clears should be safe
        context.Clear();
        context.Clear();
        context.Clear();

        // Next read should throw, not error on Clear
        var ex = Should.Throw<MissingTenantContextException>(
            () => { _ = context.CurrentTenantId; });
        ex.ShouldNotBeNull();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public void IsResolved_ReflectsCurrentState()
    {
        var context = new AmbientTenantContext();

        // Initially not resolved
        context.IsResolved.ShouldBeFalse();

        // After set
        context.TrySetTenant(555);
        context.IsResolved.ShouldBeTrue();

        // After clear
        context.Clear();
        context.IsResolved.ShouldBeFalse();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public async Task ParallelRequests_WithAndWithoutTenant_Isolated()
    {
        var context = new AmbientTenantContext();

        // Task A: set tenant
        var taskA = Task.Run(() =>
        {
            context.TrySetTenant(1000);
            var id = context.CurrentTenantId;
            return id;
        });

        // Task B: do NOT set tenant, attempt to read
        var taskB = Task.Run(() =>
        {
            try
            {
                var id = context.CurrentTenantId;
                return -1;  // Should not reach
            }
            catch (MissingTenantContextException)
            {
                return 0;  // Expected
            }
        });

        var resultA = await taskA;
        var resultB = await taskB;

        // Task A sees its tenant, Task B throws
        resultA.ShouldBe(1000);
        resultB.ShouldBe(0);  // Caught the exception as expected
    }
}
