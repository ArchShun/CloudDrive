namespace Test;

internal class TestControllerBase : ITestController
{
    public void Excute()
    {
        var tasks = new List<Task>();
        // 获取
        var methods = GetType().GetMethods()
            .Where(e => e.IsDefined(typeof(TestMethodAttribute), true) && e.IsPublic && !e.ContainsGenericParameters);
        foreach (var m in methods)
        {
            // 调用方法
            var t = m.Invoke(this, null);
            // 如果是异步方法需要统一等待
            if (t != null && t is Task) tasks.Add((Task)t);
        }
        Task.WaitAll(tasks.ToArray());
    }
}
