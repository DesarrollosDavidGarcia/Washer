using Shouldly;
using TallerPro.Security;
using Xunit;

namespace TallerPro.Application.Tests.Security;

public class TenantContextTests
{
    // -----------------------------------------------------------------
    // Test 1: reading CurrentTenantId without prior set throws
    // -----------------------------------------------------------------
    [Fact]
    public void CurrentTenantId_WhenNotSet_ThrowsMissingTenantContextException()
    {
        var sut = new AmbientTenantContext();

        var ex = Should.Throw<MissingTenantContextException>(() => _ = sut.CurrentTenantId);

        ex.Message.ShouldContain("tenant context not resolved", Case.Insensitive);
    }

    // -----------------------------------------------------------------
    // Test 2: after TrySetTenant(42), CurrentTenantId == 42
    // -----------------------------------------------------------------
    [Fact]
    public void CurrentTenantId_AfterTrySetTenant_ReturnsSetValue()
    {
        var sut = new AmbientTenantContext();

        sut.TrySetTenant(42L);

        sut.CurrentTenantId.ShouldBe(42L);
    }

    // -----------------------------------------------------------------
    // Test 3: MissingTenantContextException inherits InvalidOperationException
    // -----------------------------------------------------------------
    [Fact]
    public void MissingTenantContextException_InheritsFromInvalidOperationException()
    {
        typeof(InvalidOperationException)
            .IsAssignableFrom(typeof(MissingTenantContextException))
            .ShouldBeTrue();
    }

    // -----------------------------------------------------------------
    // Test 4: after Clear(), reading CurrentTenantId throws again
    // -----------------------------------------------------------------
    [Fact]
    public void CurrentTenantId_AfterClear_ThrowsMissingTenantContextException()
    {
        var sut = new AmbientTenantContext();
        sut.TrySetTenant(99L);

        sut.Clear();

        Should.Throw<MissingTenantContextException>(() => _ = sut.CurrentTenantId);
    }

    // -----------------------------------------------------------------
    // Test 5: Clear() is idempotent — calling it twice does not throw
    // -----------------------------------------------------------------
    [Fact]
    public void Clear_CalledTwice_DoesNotThrow()
    {
        var sut = new AmbientTenantContext();

        Should.NotThrow(() =>
        {
            sut.Clear();
            sut.Clear();
        });
    }

    // -----------------------------------------------------------------
    // Test 6: set(10) + Clear() + set(20) → CurrentTenantId == 20
    // -----------------------------------------------------------------
    [Fact]
    public void CurrentTenantId_AfterClearAndReset_ReturnsNewValue()
    {
        var sut = new AmbientTenantContext();

        sut.TrySetTenant(10L);
        sut.Clear();
        sut.TrySetTenant(20L);

        sut.CurrentTenantId.ShouldBe(20L);
    }

    // -----------------------------------------------------------------
    // Test 7: parallel isolation — two Task.Run with different tenants
    //         see only their own value (AsyncLocal isolation)
    // -----------------------------------------------------------------
    [Fact]
    public async Task TrySetTenant_InParallelTasks_IsolatesValuesPerTask()
    {
        var sut = new AmbientTenantContext();

        var task1Ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task2Ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        long task1Observed = -1;
        long task2Observed = -1;

        var t1 = Task.Run(async () =>
        {
            sut.TrySetTenant(111L);
            task1Ready.SetResult();
            await proceed.Task;
            task1Observed = sut.CurrentTenantId;
        });

        var t2 = Task.Run(async () =>
        {
            sut.TrySetTenant(222L);
            task2Ready.SetResult();
            await proceed.Task;
            task2Observed = sut.CurrentTenantId;
        });

        // Wait until both tasks have set their tenant
        var readyTimeout = Task.Delay(TimeSpan.FromSeconds(5));
        await Task.WhenAny(Task.WhenAll(task1Ready.Task, task2Ready.Task), readyTimeout);
        (task1Ready.Task.IsCompleted && task2Ready.Task.IsCompleted).ShouldBeTrue(
            "Both tasks must signal ready within 5 seconds");

        proceed.SetResult();
        await Task.WhenAll(t1, t2);

        task1Observed.ShouldBe(111L);
        task2Observed.ShouldBe(222L);
    }
}
